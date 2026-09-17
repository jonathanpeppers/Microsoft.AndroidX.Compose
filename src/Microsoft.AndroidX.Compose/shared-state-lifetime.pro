# JNI constructs this adapter; Compose calls its suspend interface method.
-keep class net.compose.PointerInputEventHandlerImpl {
    public <init>(kotlin.jvm.functions.Function2);
    public java.lang.Object invoke(androidx.compose.ui.input.pointer.PointerInputScope, kotlin.coroutines.Continuation);
}

# JNI selects this factory; Java reachability retains its SAM implementation.
-keep class composenet.compose.MeasurePolicyFactory {
    static androidx.compose.ui.layout.MeasurePolicy create(kotlin.jvm.functions.Function3);
}

# The generated JNI bridge and read-only Runtime 1.11.3 compatibility query.
-keep class composenet.compose.SharedStateLifetime {
    static boolean isLive(androidx.compose.runtime.CompositionImpl, java.lang.Object, androidx.compose.runtime.RecomposeScopeImpl, java.util.concurrent.atomic.AtomicReference, java.util.concurrent.atomic.AtomicReference, androidx.compose.runtime.CompositionImpl);
    static int dependencyCount();
    static java.util.concurrent.atomic.AtomicReference pausedOrigin(androidx.compose.runtime.CompositionImpl);
}
-keepnames class androidx.compose.runtime.RecomposeScopeImpl
# These declarations are reflected eagerly, even for an uninstantiated backend.
# R8 full mode can discard instance fields under keepclassmembers alone.
# Keep the declaring classes and only the named fields, not all their members.
-keep class androidx.compose.runtime.CompositionImpl {
    java.lang.Object lock;
    androidx.compose.runtime.Changes changes;
    androidx.compose.runtime.Changes lateChanges;
    androidx.compose.runtime.PausedCompositionImpl pendingPausedComposition;
}
-keep class androidx.compose.runtime.PausedCompositionImpl {
    java.util.concurrent.atomic.AtomicReference state;
}
-keep class androidx.compose.runtime.GapComposer {
    *** changeListWriter;
}
-keep class androidx.compose.runtime.LinkComposer {
    *** changeListWriter;
}
-keep class androidx.compose.runtime.composer.*.changelist.ComposerChangeListWriter {
    *** changeList;
}
-keep class androidx.compose.runtime.composer.*.changelist.ChangeList {
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
