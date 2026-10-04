namespace Plugin.Maui.NativeContextMenus;

static class SystemIconDrawables
{
    /// <summary>
    /// The bundled Material Symbols drawable for a <see cref="SystemIcon"/>, or 0 for None.
    /// </summary>
    public static int Id(SystemIcon icon) => icon switch
    {
        SystemIcon.Add => Resource.Drawable.ncm_ic_add,
        SystemIcon.Archive => Resource.Drawable.ncm_ic_archive,
        SystemIcon.Bookmark => Resource.Drawable.ncm_ic_bookmark,
        SystemIcon.Calendar => Resource.Drawable.ncm_ic_calendar,
        SystemIcon.Call => Resource.Drawable.ncm_ic_call,
        SystemIcon.Camera => Resource.Drawable.ncm_ic_camera,
        SystemIcon.Checkmark => Resource.Drawable.ncm_ic_checkmark,
        SystemIcon.Clock => Resource.Drawable.ncm_ic_clock,
        SystemIcon.Close => Resource.Drawable.ncm_ic_close,
        SystemIcon.Copy => Resource.Drawable.ncm_ic_copy,
        SystemIcon.Cut => Resource.Drawable.ncm_ic_cut,
        SystemIcon.Delete => Resource.Drawable.ncm_ic_delete,
        SystemIcon.Download => Resource.Drawable.ncm_ic_download,
        SystemIcon.Duplicate => Resource.Drawable.ncm_ic_duplicate,
        SystemIcon.Edit => Resource.Drawable.ncm_ic_edit,
        SystemIcon.Favorite => Resource.Drawable.ncm_ic_favorite,
        SystemIcon.Filter => Resource.Drawable.ncm_ic_filter,
        SystemIcon.Flag => Resource.Drawable.ncm_ic_flag,
        SystemIcon.Folder => Resource.Drawable.ncm_ic_folder,
        SystemIcon.Heart => Resource.Drawable.ncm_ic_heart,
        SystemIcon.Hide => Resource.Drawable.ncm_ic_hide,
        SystemIcon.Info => Resource.Drawable.ncm_ic_info,
        SystemIcon.Link => Resource.Drawable.ncm_ic_link,
        SystemIcon.Location => Resource.Drawable.ncm_ic_location,
        SystemIcon.Lock => Resource.Drawable.ncm_ic_lock,
        SystemIcon.Mail => Resource.Drawable.ncm_ic_mail,
        SystemIcon.Message => Resource.Drawable.ncm_ic_message,
        SystemIcon.More => Resource.Drawable.ncm_ic_more,
        SystemIcon.Open => Resource.Drawable.ncm_ic_open,
        SystemIcon.Paste => Resource.Drawable.ncm_ic_paste,
        SystemIcon.Pause => Resource.Drawable.ncm_ic_pause,
        SystemIcon.Person => Resource.Drawable.ncm_ic_person,
        SystemIcon.Photo => Resource.Drawable.ncm_ic_photo,
        SystemIcon.Pin => Resource.Drawable.ncm_ic_pin,
        SystemIcon.Play => Resource.Drawable.ncm_ic_play,
        SystemIcon.Print => Resource.Drawable.ncm_ic_print,
        SystemIcon.Refresh => Resource.Drawable.ncm_ic_refresh,
        SystemIcon.Remove => Resource.Drawable.ncm_ic_remove,
        SystemIcon.Reply => Resource.Drawable.ncm_ic_reply,
        SystemIcon.Search => Resource.Drawable.ncm_ic_search,
        SystemIcon.Send => Resource.Drawable.ncm_ic_send,
        SystemIcon.Settings => Resource.Drawable.ncm_ic_settings,
        SystemIcon.Share => Resource.Drawable.ncm_ic_share,
        SystemIcon.Show => Resource.Drawable.ncm_ic_show,
        SystemIcon.Sort => Resource.Drawable.ncm_ic_sort,
        SystemIcon.Unlock => Resource.Drawable.ncm_ic_unlock,
        _ => 0,
    };
}
