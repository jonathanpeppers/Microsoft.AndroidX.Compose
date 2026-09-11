# The generated JNI bridge and read-only Runtime 1.11.3 compatibility query.
-keep class composenet.compose.SharedStateLifetime {
    static boolean isLive(androidx.compose.runtime.CompositionImpl, java.lang.Object, androidx.compose.runtime.RecomposeScopeImpl, java.util.concurrent.atomic.AtomicReference, java.util.concurrent.atomic.AtomicReference);
    static java.util.concurrent.atomic.AtomicReference pausedOrigin(androidx.compose.runtime.CompositionImpl);
}
-keepnames class androidx.compose.runtime.CompositionImpl
-keepnames class androidx.compose.runtime.RecomposeScopeImpl
-keepclassmembers class androidx.compose.runtime.CompositionImpl {
    java.lang.Object lock;
    androidx.compose.runtime.Changes changes;
    androidx.compose.runtime.Changes lateChanges;
    androidx.compose.runtime.PausedCompositionImpl pendingPausedComposition;
}
-keepclassmembers class androidx.compose.runtime.PausedCompositionImpl {
    java.util.concurrent.atomic.AtomicReference state;
}
-keepclassmembers class androidx.compose.runtime.GapComposer {
    *** changeListWriter;
}
-keepclassmembers class androidx.compose.runtime.LinkComposer {
    *** changeListWriter;
}
-keepclassmembers class androidx.compose.runtime.composer.*.changelist.ComposerChangeListWriter {
    *** changeList;
}
-keepclassmembers class androidx.compose.runtime.composer.*.changelist.ChangeList {
    *** operations;
}
-keepnames class androidx.compose.runtime.composer.*.changelist.Operation$Remember
-keepnames class androidx.compose.runtime.composer.*.changelist.Operation$UpdateValue
-keepnames class androidx.compose.runtime.composer.*.changelist.Operation$UpdateAnchoredValue
-keepnames class androidx.compose.runtime.composer.*.changelist.Operation$UpdateValueRelative
-keepnames class androidx.compose.runtime.composer.*.changelist.Operation$AppendValue
-keepnames class androidx.compose.runtime.composer.*.changelist.Operation$ApplyChangeList

# Shared-state factories reached only through generated JNI bridges.
-keep class androidx.compose.material3.TimePickerKt {
    public static androidx.compose.material3.TimePickerState rememberTimePickerState(int, int, boolean, androidx.compose.runtime.Composer, int, int);
    public static void TimeInput(androidx.compose.material3.TimePickerState, androidx.compose.ui.Modifier, androidx.compose.material3.TimePickerColors, androidx.compose.runtime.Composer, int, int);
    public static void TimePicker-mT9BvqQ(androidx.compose.material3.TimePickerState, androidx.compose.ui.Modifier, androidx.compose.material3.TimePickerColors, int, androidx.compose.runtime.Composer, int, int);
}
-keep class androidx.compose.material3.SheetDefaultsKt {
    public static androidx.compose.material3.SheetState rememberSheetState-AGcomas(boolean, kotlin.jvm.functions.Function1, androidx.compose.material3.SheetValue, boolean, float, float, androidx.compose.runtime.Composer, int, int);
}
