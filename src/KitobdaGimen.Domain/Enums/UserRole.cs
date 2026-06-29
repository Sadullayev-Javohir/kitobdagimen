namespace KitobdaGimen.Domain.Enums;

/// <summary>Platform authorization role. Higher value = more privileges.</summary>
public enum UserRole
{
    /// <summary>Oddiy foydalanuvchi.</summary>
    User = 0,

    /// <summary>Admin — istalgan post va iqtibosni o'chira oladi (moderatsiya).</summary>
    Admin = 1,

    /// <summary>Super admin — admin huquqlari + foydalanuvchilarni admin qilish/olib tashlash
    /// va foydalanuvchilarni butunlay o'chirish.</summary>
    SuperAdmin = 2
}
