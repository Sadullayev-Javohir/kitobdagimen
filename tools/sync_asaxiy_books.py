#!/usr/bin/env python3
"""
Asaxiy.uz saytidan kitoblarni o'qib, PostgreSQL bazasiga yuklovchi (seeder / sync) skript.
Ishlatish:
    python3 tools/sync_asaxiy_books.py --pages 10 --db local
    python3 tools/sync_asaxiy_books.py --pages 20 --db prod
"""

import sys
import os
import argparse
import json
import re
import urllib.request
import subprocess

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
    # "Muallif: Sarlavha"
    idx = name.find(':')
    if 0 < idx < len(name) - 1:
        a = unwrap_quotes(name[:idx])
        t = unwrap_quotes(name[idx+1:])
        if a and t:
            return a, t
    # "Muallif - Sarlavha"
    dash = name.find(" - ")
    if 0 < dash < len(name) - 3:
        p1 = unwrap_quotes(name[:dash])
        p2 = unwrap_quotes(name[dash+3:])
        if p1 and p2:
            return p1, p2
    return "Noma'lum muallif", unwrap_quotes(name)

def clean_title(title: str) -> str:
    # "PREMYERA!", "YANGILIK!" kabi marketing so'zlarini tozalash
    t = re.sub(r'\b(PREMYERA|YANGILIK|XIT|TOP)\s*!?\b', '', title, flags=re.IGNORECASE).strip()
    t = re.sub(r'\s+', ' ', t).strip(' -.,:')
    return t or title

def fetch_page(page: int):
    url = f"https://asaxiy.uz/product/knigi?page={page}"
    req = urllib.request.Request(
        url,
        headers={
            "User-Agent": "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            "Accept-Language": "uz,en;q=0.9"
        }
    )
    try:
        with urllib.request.urlopen(req, timeout=15) as res:
            html = res.read().decode("utf-8")
    except Exception as e:
        print(f"[Xato] Sahifa {page} olinmadi: {e}", file=sys.stderr)
        return []

    # Schema.org JSON-LD dan o'qish
    matches = re.findall(r'<script type=["\']application/ld\+json["\']>(.*?)</script>', html, re.DOTALL)
    books = []
    seen = set()

    for m in matches:
        if "ItemList" not in m:
            continue
        try:
            data = json.loads(m.strip())
            items = data.get("itemListElement", [])
            for it in items:
                raw_name = it.get("name", "").strip()
                raw_url = it.get("url", "").strip()
                raw_img = it.get("image", "").strip()
                if not raw_name or not raw_url:
                    continue

                author, title = split_name(raw_name)
                title = clean_title(title)
                if not title or len(title) < 2:
                    continue

                author = author[:100]
                title = title[:100]
                key = (title.lower(), author.lower())
                if key in seen:
                    continue
                seen.add(key)

                books.append({
                    "title": title,
                    "author": author,
                    "cover_url": raw_img,
                    "source": "asaxiy.uz"
                })
        except Exception:
            continue

    return books

def escape_sql(s: str) -> str:
    return s.replace("'", "''")

def main():
    parser = argparse.ArgumentParser(description="Asaxiy.uz dan kitoblarni Postgres'ga yuklash")
    parser.add_argument("--pages", type=int, default=10, help="Yuklanadigan sahifalar soni (har birida 24 ta kitob)")
    parser.add_argument("--target", choices=["local", "prod", "both"], default="local", help="Qaysi bazaga yuklash")
    args = parser.parse_args()

    print(f"[*] Asaxiy.uz dan {args.pages} ta sahifa yuklanmoqda...")
    all_books = []
    seen_keys = set()

    for p in range(1, args.pages + 1):
        items = fetch_page(p)
        added = 0
        for b in items:
            k = (b["title"].lower(), b["author"].lower())
            if k not in seen_keys:
                seen_keys.add(k)
                all_books.append(b)
                added += 1
        print(f"  Sahifa {p:2d}: {len(items)} ta kitob topildi, {added} ta yangi qo'shildi.")

    print(f"[✓] Jami olingan yangi kitoblar: {len(all_books)} ta.")

    # SQL batch yaratish
    # Agar kitob (Title va Author bo'yicha) mavjud bo'lmasa INSERT qilamiz.
    sql_lines = [
        "BEGIN;",
        "CREATE TEMP TABLE tmp_asaxiy_books (title text, author text, cover text, source text) ON COMMIT DROP;"
    ]

    for b in all_books:
        t = escape_sql(b["title"])
        a = escape_sql(b["author"])
        c = escape_sql(b["cover_url"])
        s = escape_sql(b["source"])
        sql_lines.append(f"INSERT INTO tmp_asaxiy_books VALUES ('{t}', '{a}', '{c}', '{s}');")

    sql_lines.append("""
    INSERT INTO "Books" ("Title", "Author", "CoverUrl", "TotalPages", "Source", "GenreId")
    SELECT t.title, t.author, t.cover, 1, t.source, NULL
    FROM tmp_asaxiy_books t
    WHERE NOT EXISTS (
        SELECT 1 FROM "Books" b 
        WHERE LOWER(TRIM(b."Title")) = LOWER(TRIM(t.title)) 
          AND LOWER(TRIM(b."Author")) = LOWER(TRIM(t.author))
    );
    """)
    sql_lines.append("COMMIT;")
    sql_content = "\n".join(sql_lines)

    sql_file = "/tmp/asaxiy_seed.sql"
    with open(sql_file, "w", encoding="utf-8") as f:
        f.write(sql_content)

    if args.target in ("local", "both"):
        print("[*] Lokal bazaga (localhost:5435) yuklanmoqda...")
        env = os.environ.copy()
        env["PGPASSWORD"] = "englishai_dev"
        res = subprocess.run(
            ["psql", "-h", "localhost", "-p", "5435", "-U", "englishai", "-d", "kitobdagimen", "-f", sql_file],
            env=env, capture_output=True, text=True
        )
        if res.returncode == 0:
            print("[✓] Lokal bazaga muvaffaqiyatli yuklandi.")
        else:
            print(f"[XATO] Lokal bazaga yuklashda xatolik: {res.stderr}")

    if args.target in ("prod", "both"):
        print("[*] Production bazaga (Hetzner Finland) yuklanmoqda...")
        # Serverga faylni nusxalash va docker orqali psql yurgizish
        scp_res = subprocess.run(["scp", sql_file, "root@37.27.6.193:/tmp/asaxiy_seed.sql"], capture_output=True, text=True)
        if scp_res.returncode == 0:
            remote_cmd = "docker cp /tmp/asaxiy_seed.sql kitobdagimen-db:/tmp/asaxiy_seed.sql && docker exec kitobdagimen-db psql -U kitobdagimen -d kitobdagimen -f /tmp/asaxiy_seed.sql"
            run_res = subprocess.run(["ssh", "root@37.27.6.193", remote_cmd], capture_output=True, text=True)
            if run_res.returncode == 0:
                print("[✓] Production bazaga muvaffaqiyatli yuklandi.")
            else:
                print(f"[XATO] Production bazaga yuklashda xatolik: {run_res.stderr}")
        else:
            print(f"[XATO] Serverga fayl nusxalanmadi: {scp_res.stderr}")

if __name__ == "__main__":
    main()

