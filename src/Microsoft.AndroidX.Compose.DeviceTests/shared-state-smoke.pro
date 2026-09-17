# The test runner selects both backends through a managed binding's JNI field
# setter. CoreCLR's R8 inputs do not otherwise retain this Java static field.
-keep class androidx.compose.runtime.ComposeRuntimeFlags {
    public static boolean isLinkBufferComposerEnabled;
}
# The transaction tests inspect the current native veto through raw JNI.
-keepclassmembers class androidx.compose.material3.DrawerState {
    public kotlin.jvm.functions.Function1 getConfirmStateChange$material3();
}
