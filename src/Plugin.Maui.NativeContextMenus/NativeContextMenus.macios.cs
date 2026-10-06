using Foundation;
using System.Collections.Concurrent;
using UIKit;

namespace Plugin.Maui.NativeContextMenus;

#if IOS || MACCATALYST
partial class NativeContextMenusImplementation : INativeContextMenus
{
}
#endif