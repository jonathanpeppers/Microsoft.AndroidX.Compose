using System.Text;
using AccessibilityNodeInfo = Android.Views.Accessibility.AccessibilityNodeInfo;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal static class FlowTestEvidence
{
    internal static string Describe(FlowOverflowTestActivity activity, AccessibilityNodeInfo root, string target)
    {
        Assert.AreEqual("net.compose.devicetests", root.PackageName);
        Assert.AreEqual(activity.Admission.WindowId, root.WindowId);
        string state = "";
        FlowTestAdmission.OnUi(() =>
        {
            activity.Admission.RequireLive(activity);
            state = $"instance={activity.Admission.InstanceId} pid={activity.Admission.NativePid} " +
                $"window={activity.Admission.WindowId} target={target} generation={activity.Generation.Value} " +
                $"lines={activity.Lines.Value} total={activity.Total.Value} clicks={activity.Clicks} " +
                $"snapshot={activity.Last}";
        });
        var text = new StringBuilder().AppendLine("FLOW_TARGET_FAILURE " + state);
        int remaining = 100;
        Append(root, text, 0, ref remaining);
        return text.ToString();
    }

    static void Append(AccessibilityNodeInfo node, StringBuilder text, int depth, ref int remaining)
    {
        if (remaining-- <= 0 || depth > 12)
        {
            text.AppendLine("[bounded tree truncated]");
            return;
        }
        text.Append(' ', depth).Append("package=").Append(node.PackageName)
            .Append(" window=").Append(node.WindowId);
        if (node.PackageName != "net.compose.devicetests")
        {
            text.AppendLine(" [foreign boundary; content not inspected]");
            return;
        }
        using var bounds = new global::Android.Graphics.Rect();
        node.GetBoundsInScreen(bounds);
        var actions = node.ActionList ?? [];
        text.Append(" class=").Append(node.ClassName)
            .Append(" description=").Append(node.ContentDescription)
            .Append(" text=").Append(node.Text)
            .Append(" clickable=").Append(node.Clickable)
            .Append(" enabled=").Append(node.Enabled)
            .Append(" visible=").Append(node.VisibleToUser)
            .Append(" actions=").AppendJoin(",", actions.Select(action => action.Id))
            .Append(" bounds=").Append(bounds)
            .Append(" children=").Append(node.ChildCount).AppendLine();
        for (int i = 0; i < node.ChildCount && remaining > 0; i++)
        {
            using var child = node.GetChild(i);
            if (child is not null) Append(child, text, depth + 1, ref remaining);
        }
    }
}
