#!/usr/bin/env python3
"""
Asaxiy.uz saytidan barcha kitoblarni (Nomi, Muallifi, Betlar soni va .webp muqovasi bilan)
to'liq yuklab oluvchi va PostgreSQL bazasiga hamda server diskiga joylovchi vosita.

Imkoniyatlari:
- Faqat haqiqiy kitoblar (Betlar soni >= 5) olinadi. Soxta / kantselyariya buyumlari tashlab ketiladi.
- Muqova rasmlari yuklanib, ixchamlashtiriladi va .webp formatda saqlanadi.
- Har bir partiya (batch) yuklangach, darhol bazaga yoziladi va serverga sinxronlanadi (avariya/to'xtash holatlarida ham saqlanib qoladi).
- Avval yuklangan kitoblar qayta yuklanmaydi (duplicate prevention).

Ishlatish:
    python3 tools/import_books_with_pages.py --start-page 1 --end-page 398 --batch-size 5 --target both
"""

import sys
import os
import argparse
import json
import re
import uuid
import io
import time
import urllib.request
import subprocess
from concurrent.futures import ThreadPoolExecutor, as_completed
from PIL import Image

HEADERS = {
    "User-Agent": "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
    "Accept-Language": "uz,en;q=0.9"
}

def unwrap_quotes(s: str) -> str:
    s = s.strip()
    if len(s) >= 2 and (
        (s[0] == '"' and s[-1] == '"') or
        (s[0] == "'" and s[-1] == "'") or
        (s[0] == '«' and s[-1] == '»') or
        (s[0] == '“' and s[-1] == '”')
    ):
        return s[1:-1].strip()
    return s

def split_name(name: str):
    name = name.strip()
    idx = name.find(':')
    if 0 < idx < len(name) - 1:
        a = unwrap_quotes(name[:idx])
        t = unwrap_quotes(name[idx+1:])
        if a and t:
            return a, t
    dash = name.find(" - ")
    if 0 < dash < len(name) - 3:
        p1 = unwrap_quotes(name[:dash])
        p2 = unwrap_quotes(name[dash+3:])
        if p1 and p2:
            return p1, p2
    return "Noma'lum muallif", unwrap_quotes(name)

def clean_title(title: str) -> str:
    t = re.sub(r'\b(PREMYERA|YANGILIK|XIT|TOP)\s*!?\b', '', title, flags=re.IGNORECASE).strip()
    t = re.sub(r'\s+', ' ', t).strip(' -.,:')
    return t or title

def fetch_url(url: str, retries: int = 2) -> str:
    for attempt in range(retries + 1):
        try:
            req = urllib.request.Request(url, headers=HEADERS)
            with urllib.request.urlopen(req, timeout=12) as res:
                return res.read().decode("utf-8")
        except Exception:
            if attempt < retries:
                time.sleep(1.0)
    return ""

def fetch_catalog_urls(page: int):
    url = f"https://asaxiy.uz/product/knigi?page={page}"
    html = fetch_url(url)
    if not html:
        return []

    urls = []
    matches = re.findall(r'<script type=["\']application/ld\+json["\']>(.*?)</script>', html, re.DOTALL)
    for m in matches:
        if "ItemList" not in m:
            continue
        try:
            data = json.loads(m.strip())
            for item in data.get("itemListElement", []):
                u = item.get("url")
                if u and "/product/" in u:
                    urls.append(u.strip())
        except Exception:
            continue
    return urls

def process_book_detail(url: str, covers_dir: str):
    html = fetch_url(url)
    if not html:
        return None

    # 1. Betlar sonini olish (TotalPages)
    pages = 0
    m_pages = re.search(r"(?:Betlar\s+soni|Страниц|Sahifalar\s+soni)</td>\s*<td[^>]*>(.*?)</td>", html, re.IGNORECASE | re.DOTALL)
    if not m_pages:
        m_pages = re.search(r"(?:Betlar\s+soni|Страниц|Sahifalar\s+soni)\s*[:\-–]?\s*<[^>]+>\s*(\d[\d\s]*\d|\d+)", html, re.IGNORECASE)
    if m_pages:
        digits = re.sub(r"[^\d]", "", m_pages.group(1))
        if digits:
            pages = int(digits)

    # Agar betlar soni aniqlanmasa yoki 5 dan kam bo'lsa (kitob emas yoki daftar/xatcho'p) -> SKIP!
    if pages < 5 or pages > 15000:
        return None

    # 2. Nom va Muallifni olish
    raw_name = ""
    raw_img = ""
    for m in re.finditer(r'<script type=["\']application/ld\+json["\']>(.*?)</script>', html, re.DOTALL):
        if "Product" in m.group(1):
            try:
                data = json.loads(m.group(1))
                if data.get("@type") == "Product":
                    raw_name = data.get("name", "")
                    raw_img = data.get("image", "")
                    break
            except Exception:
                pass

    if not raw_name:
        m_title = re.search(r'<h1[^>]*class=["\'][^"\']*product-title[^"\']*["\'][^>]*>(.*?)</h1>', html, re.IGNORECASE | re.DOTALL)
        if m_title:
            raw_name = re.sub(r'<[^>]+>', '', m_title.group(1)).strip()

    if not raw_name:
        return None

    author, title = split_name(raw_name)
    title = clean_title(title)
    if not title or len(title) < 2:
        return None

    title = title[:295]
    author = author[:195]

    # 3. Muqova rasmini yuklab, .webp ga o'tkazish
    cover_rel_path = None
    if raw_img and raw_img.startswith("http"):
        try:
            img_req = urllib.request.Request(raw_img, headers=HEADERS)
            with urllib.request.urlopen(img_req, timeout=10) as img_res:
                img_data = img_res.read()
            
            with Image.open(io.BytesIO(img_data)) as im:
                im = im.convert("RGB")
                im.thumbnail((800, 1200), Image.Resampling.LANCZOS)
                file_id = f"{uuid.uuid4().hex}.webp"
                local_file = os.path.join(covers_dir, file_id)
                im.save(local_file, format="WEBP", quality=80)
                cover_rel_path = f"/uploads/covers/{file_id}"
        except Exception:
            cover_rel_path = None

    return {
        "title": title,
        "author": author,
        "total_pages": pages,
        "cover_url": cover_rel_path,
        "source": "asaxiy.uz"
    }

def escape_sql(s: str) -> str:
    return s.replace("'", "''")

def get_existing_books(conn_str_env: dict):
    cmd = ["psql", "-h", "localhost", "-p", "5435", "-U", "englishai", "-d", "kitobdagimen", "-t", "-c", 
           'SELECT LOWER(TRIM("Title")), LOWER(TRIM("Author")) FROM "Books";']
    res = subprocess.run(cmd, env=conn_str_env, capture_output=True, text=True)
    existing = set()
    if res.returncode == 0:
        for line in res.stdout.splitlines():
            parts = line.split("|")
            if len(parts) >= 2:
                existing.add((parts[0].strip(), parts[1].strip()))
    return existing

def flush_batch(batch_books, target, local_env, local_covers_dir):
    if not batch_books:
        return

    sql_lines = [
        "BEGIN;",
        "CREATE TEMP TABLE tmp_new_books (title text, author text, pages int, cover text, source text) ON COMMIT DROP;"
    ]
    for b in batch_books:
        t = escape_sql(b["title"])
        a = escape_sql(b["author"])
        p = b["total_pages"]
        c = f"'{escape_sql(b['cover_url'])}'" if b["cover_url"] else "NULL"
        s = escape_sql(b["source"])
        sql_lines.append(f"INSERT INTO tmp_new_books VALUES ('{t}', '{a}', {p}, {c}, '{s}');")

    sql_lines.append("""
    INSERT INTO "Books" ("Title", "Author", "TotalPages", "CoverUrl", "Source", "GenreId")
    SELECT t.title, t.author, t.pages, t.cover, t.source, NULL
    FROM tmp_new_books t
    WHERE NOT EXISTS (
        SELECT 1 FROM "Books" b 
        WHERE LOWER(TRIM(b."Title")) = LOWER(TRIM(t.title)) 
          AND LOWER(TRIM(b."Author")) = LOWER(TRIM(t.author))
    );
    """)
    sql_lines.append("COMMIT;")
    sql_content = "\n".join(sql_lines)

    sql_file = "/tmp/asaxiy_batch.sql"
    with open(sql_file, "w", encoding="utf-8") as f:
        f.write(sql_content)

    if target in ("local", "both"):
        subprocess.run(
            ["psql", "-h", "localhost", "-p", "5435", "-U", "englishai", "-d", "kitobdagimen", "-f", sql_file],
            env=local_env, capture_output=True, text=True
        )

    if target in ("prod", "both"):
        # Rsync covers
        subprocess.run(
            ["rsync", "-avz", "--update", f"{local_covers_dir}/", "root@37.27.6.193:/opt/kitobdagimen/shared/uploads/covers/"],
            capture_output=True, text=True
        )
        # Execute SQL on production
        scp_res = subprocess.run(["scp", sql_file, "root@37.27.6.193:/tmp/asaxiy_batch.sql"], capture_output=True, text=True)
        if scp_res.returncode == 0:
            remote_cmd = "docker cp /tmp/asaxiy_batch.sql kitobdagimen-db:/tmp/asaxiy_batch.sql && docker exec kitobdagimen-db psql -U kitobdagimen -d kitobdagimen -f /tmp/asaxiy_batch.sql"
            subprocess.run(["ssh", "root@37.27.6.193", remote_cmd], capture_output=True, text=True)

def main():
    parser = argparse.ArgumentParser(description="Asaxiy'dan barcha kitoblarni to'liq yuklash")
    parser.add_argument("--start-page", type=int, default=1, help="Boshlang'ich sahifa")
    parser.add_argument("--end-page", type=int, default=398, help="Oxirgi sahifa")
    parser.add_argument("--batch-size", type=int, default=5, help="Har necha sahifada bazaga yozish")
    parser.add_argument("--target", choices=["local", "prod", "both"], default="both", help="Qayerga yuklash")
    parser.add_argument("--workers", type=int, default=12, help="Parallel oqimlar soni")
    args = parser.parse_args()

    local_covers_dir = os.path.abspath("src/KitobdaGimen.Web/wwwroot/uploads/covers")
    os.makedirs(local_covers_dir, exist_ok=True)

    local_env = os.environ.copy()
    local_env["PGPASSWORD"] = "englishai_dev"
    existing = get_existing_books(local_env)
    print(f"[*] Boshlashdan avval bazada {len(existing)} ta kitob mavjud.")
    print(f"[*] Sahifalar: {args.start_page} dan {args.end_page} gacha (Jami {args.end_page - args.start_page + 1} ta sahifa).")

    total_added = 0
    current_page = args.start_page

    while current_page <= args.end_page:
        batch_end = min(current_page + args.batch_size - 1, args.end_page)
        print(f"\n[Partiya] Sahifalar {current_page} - {batch_end} o'qilmoqda...")

        urls = []
        for p in range(current_page, batch_end + 1):
            p_urls = fetch_catalog_urls(p)
            urls.extend(p_urls)

        urls = list(dict.fromkeys(urls))
        print(f"  {len(urls)} ta kitob havolasi topildi. Tafsilotlar va WebP muqovalar yuklanmoqda...")

        batch_books = []
        with ThreadPoolExecutor(max_workers=args.workers) as executor:
            future_to_url = {executor.submit(process_book_detail, u, local_covers_dir): u for u in urls}
            for future in as_completed(future_to_url):
                book = future.result()
                if not book:
                    continue

                k = (book["title"].lower().strip(), book["author"].lower().strip())
                if k in existing:
                    continue
                existing.add(k)
                batch_books.append(book)

        flush_batch(batch_books, args.target, local_env, local_covers_dir)
        total_added += len(batch_books)
        print(f"[✓] Sahifalar {current_page}-{batch_end} muvaffaqiyatli saqlandi (+{len(batch_books)} ta yangi kitob). Jami yangi qo'shilgan: {total_added} ta.")

        current_page = batch_end + 1
        time.sleep(0.5)

    print(f"\n[TABRIKLAYMIZ] Jarayon to'liq yakunlandi! Jami qo'shilgan kitoblar: {total_added} ta.")

if __name__ == "__main__":
    main()
