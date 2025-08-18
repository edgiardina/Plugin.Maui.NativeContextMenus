using Foundation;
using System.Collections.Concurrent;
using UIKit;

namespace Plugin.Maui.NativeContextMenus;

#if IOS || MACCATALYST
partial class NativeContextMenusImplementation : INativeContextMenus
{
    // iOS/macOS specific implementation using the injector approach
    // The actual functionality is handled by NativeContextMenuToolbarInjector.macios.cs
}
#endif