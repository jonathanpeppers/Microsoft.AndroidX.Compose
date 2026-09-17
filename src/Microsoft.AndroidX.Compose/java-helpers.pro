# JNI constructs this adapter; Compose calls its suspend interface method.
-keep class net.compose.PointerInputEventHandlerImpl {
    public <init>(kotlin.jvm.functions.Function2);
    public java.lang.Object invoke(androidx.compose.ui.input.pointer.PointerInputScope, kotlin.coroutines.Continuation);
}

# JNI selects this factory; Java reachability retains its SAM implementation.
-keep class composenet.compose.MeasurePolicyFactory {
    static androidx.compose.ui.layout.MeasurePolicy create(kotlin.jvm.functions.Function3);
}
