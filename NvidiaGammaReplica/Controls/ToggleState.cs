using System.Windows;

namespace NvidiaGammaReplica.Controls;

/// <summary>
/// Attached property used to flag a control (e.g. a preset button) as the currently
/// active selection so styles can light it up. Kept generic so it can be reused.
/// </summary>
public static class ToggleState
{
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.RegisterAttached(
            "IsActive",
            typeof(bool),
            typeof(ToggleState),
            new PropertyMetadata(false));

    public static void SetIsActive(DependencyObject element, bool value) =>
        element.SetValue(IsActiveProperty, value);

    public static bool GetIsActive(DependencyObject element) =>
        (bool)element.GetValue(IsActiveProperty);
}
