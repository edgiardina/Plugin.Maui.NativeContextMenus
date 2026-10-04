namespace Plugin.Maui.NativeContextMenus;

public static partial class NativeContextMenus
{
	static INativeContextMenus? defaultImplementation;

	/// <summary>
	/// Provides the default implementation for static usage of this API.
	/// </summary>
	public static INativeContextMenus Default =>
		defaultImplementation ??= new NativeContextMenusImplementation();

	internal static void SetDefault(INativeContextMenus? implementation) =>
		defaultImplementation = implementation;

	/// <summary>
	/// Attaches a native context menu to a view.
	/// </summary>
	public static readonly BindableProperty ContextMenuProperty =
		BindableProperty.CreateAttached(
			"ContextMenu",
			typeof(NativeContextMenu),
			typeof(NativeContextMenus),
			defaultValue: null,
			propertyChanged: OnContextMenuChanged);

	public static NativeContextMenu? GetContextMenu(BindableObject view) =>
		(NativeContextMenu?)view.GetValue(ContextMenuProperty);

	public static void SetContextMenu(BindableObject view, NativeContextMenu? value) =>
		view.SetValue(ContextMenuProperty, value);

	/// <summary>
	/// Opens the context menu of a view. This does nothing if the view has no menu or is not on screen.
	/// </summary>
	public static void Show(BindableObject view)
	{
		if (view is VisualElement element &&
			GetContextMenu(element) is { HasVisibleItems: true } &&
			element.Handler?.PlatformView is { } platformView)
			PlatformShow(element, platformView);
	}

	// Attaches the menu again after a change that the platform code reads only at attach time
	internal static void Reattach(NativeContextMenu menu)
	{
		if (menu.Parent is not VisualElement element ||
			GetContextMenu(element) != menu ||
			element.Handler?.PlatformView is not { } platformView)
			return;

		PlatformDetach(platformView);
		PlatformAttach(element, platformView);
	}

	static void OnContextMenuChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		if (bindable is not VisualElement element)
			return;

		element.BindingContextChanged -= OnElementBindingContextChanged;
		element.HandlerChanging -= OnElementHandlerChanging;
		element.HandlerChanged -= OnElementHandlerChanged;

		if (oldValue is NativeContextMenu oldMenu && oldMenu.Parent == element)
		{
			oldMenu.Parent = null;
			BindableObject.SetInheritedBindingContext(oldMenu, null);
		}

		if (newValue is not NativeContextMenu newMenu)
		{
			PlatformDetach(element.Handler?.PlatformView);
			return;
		}

		// Parent lets RelativeSource bindings in the menu find ancestors of the view
		newMenu.Parent = element;
		BindableObject.SetInheritedBindingContext(newMenu, element.BindingContext);

		element.BindingContextChanged += OnElementBindingContextChanged;
		element.HandlerChanging += OnElementHandlerChanging;
		element.HandlerChanged += OnElementHandlerChanged;

		if (element.Handler?.PlatformView is { } platformView)
			PlatformAttach(element, platformView);
	}

	static void OnElementBindingContextChanged(object? sender, EventArgs e)
	{
		if (sender is VisualElement element && GetContextMenu(element) is { } menu)
			BindableObject.SetInheritedBindingContext(menu, element.BindingContext);
	}

	static void OnElementHandlerChanging(object? sender, HandlerChangingEventArgs e) =>
		PlatformDetach(e.OldHandler?.PlatformView);

	static void OnElementHandlerChanged(object? sender, EventArgs e)
	{
		if (sender is VisualElement element && element.Handler?.PlatformView is { } platformView)
			PlatformAttach(element, platformView);
	}

	static partial void PlatformAttach(VisualElement element, object platformView);

	static partial void PlatformDetach(object? platformView);

	static partial void PlatformShow(VisualElement element, object platformView);
}
