package composenet.compose;

import java.lang.reflect.Field;
import java.lang.ref.WeakReference;
import java.util.ArrayList;
import java.util.Collections;
import java.util.IdentityHashMap;
import java.util.Set;
import java.util.concurrent.atomic.AtomicReference;

import androidx.compose.runtime.CompositionImpl;
import androidx.compose.runtime.PausedCompositionImpl;
import androidx.compose.runtime.PausedCompositionState;
import androidx.compose.runtime.RecomposeScopeImpl;
import androidx.compose.runtime.RememberObserverHolder;

/**
 * Read-only compatibility with the pinned Compose Runtime 1.11.3 operation
 * queues. A registration is removed from these queues before fallible
 * abandonment callbacks; phase flags and retained insertion tables do not
 * provide that guarantee.
 */
final class SharedStateLifetime {
    private static final Field lock = field(CompositionImpl.class, "lock");
    private static final Field changes = field(CompositionImpl.class, "changes");
    private static final Field lateChanges = field(CompositionImpl.class, "lateChanges");
    private static final Field pendingPausedComposition = field(CompositionImpl.class, "pendingPausedComposition");
    private static final Field pausedState = field(PausedCompositionImpl.class, "state");
    private static final Field gapWriter = field(androidx.compose.runtime.GapComposer.class, "changeListWriter");
    private static final Field linkWriter = field(androidx.compose.runtime.LinkComposer.class, "changeListWriter");
    private static final Field gapList = field(
        androidx.compose.runtime.composer.gapbuffer.changelist.ComposerChangeListWriter.class, "changeList");
    private static final Field linkList = field(
        androidx.compose.runtime.composer.linkbuffer.changelist.ComposerChangeListWriter.class, "changeList");
    private static final Field gapOperations = field(
        androidx.compose.runtime.composer.gapbuffer.changelist.ChangeList.class, "operations");
    private static final Field linkOperations = field(
        androidx.compose.runtime.composer.linkbuffer.changelist.ChangeList.class, "operations");
    private static final Object dependencyGate = new Object();
    private static final IdentityHashMap<Object, Object> dependencies = new IdentityHashMap<>();
    private static final ArrayList<WeakReference<Object>> monitors = new ArrayList<>();

    private SharedStateLifetime() { }

    static AtomicReference<?> pausedOrigin(CompositionImpl composition) {
        try {
            Object monitor = lock.get(composition);
            synchronized (dependencyGate) {
                observeMonitor(monitor);
            }
            synchronized (monitor) {
                Object paused = pendingPausedComposition.get(composition);
                // The cell is never replaced and retains only an enum value, not
                // the completed transaction's original content and composition.
                return paused == null ? null : (AtomicReference<?>) pausedState.get(paused);
            }
        } catch (IllegalAccessException error) {
            throw new IllegalStateException("Cannot capture pinned Compose shared-state origin.", error);
        }
    }

    static boolean isLive(CompositionImpl composition, Object token, RecomposeScopeImpl scope,
            AtomicReference<?> registrationOrigin, AtomicReference<?> ownershipOrigin, CompositionImpl consumer) {
        try {
            Object target = lock.get(composition);
            Object source = consumer == null ? null : lock.get(consumer);
            ArrayList<Object> tracked = beginDependencies(source, target);
            try {
                // Never wait for a native monitor while holding dependencyGate.
                synchronized (target) {
                    try {
                        // Paused cancellation can leave installed slots. Qualify all
                        // membership results with the captured cancellation cells.
                        return !composition.isDisposed() && registered(composition, token, scope)
                            && (registrationOrigin == null || registrationOrigin.get() != PausedCompositionState.Cancelled)
                            && (ownershipOrigin == null || ownershipOrigin.get() != PausedCompositionState.Cancelled);
                    } finally {
                        if (tracked != null) {
                            endDependencies(tracked);
                            tracked = null;
                        }
                    }
                }
            } finally {
                // Also covers failure before monitor entry. Normally the edge is
                // removed while target is still held, so it cannot look stale.
                if (tracked != null)
                    endDependencies(tracked);
            }
        } catch (IllegalAccessException error) {
            throw new IllegalStateException("Cannot inspect pinned Compose shared-state lifetime.", error);
        }
    }

    private static ArrayList<Object> beginDependencies(Object source, Object target) {
        // Actual Java monitor identity/ownership, not managed peer equality.
        // An already-held target is reentrant and cannot introduce a wait.
        synchronized (dependencyGate) {
            if (!findMonitor(target))
                throw new IllegalStateException("Shared state owner monitor was not registered before publication.");
            if (Thread.holdsLock(target))
                return null;
            if (source != null)
                observeMonitor(source);
            ArrayList<Object> held = new ArrayList<>();
            // A composition can be nested inside another on this thread. Every
            // borrowable owner publishes its monitor before its token, so include
            // ALL known monitors actually held here, not just the inner consumer.
            for (WeakReference<Object> reference : monitors) {
                Object monitor = reference.get();
                if (monitor != null && Thread.holdsLock(monitor))
                    held.add(monitor);
            }
            for (Object monitor : held) {
                Object next = target;
                while (next != null) {
                    if (next == monitor)
                        throw new IllegalStateException(
                            "Shared state ownership cycle detected between concurrent compositions. Retry composition sequentially.");
                    next = dependencies.get(next);
                }
                // This native query invokes no user code while holding target.
                if (dependencies.containsKey(monitor))
                    throw new IllegalStateException("Shared state monitor already has an active dependency.");
            }
            try {
                for (Object monitor : held)
                    dependencies.put(monitor, target);
            } catch (RuntimeException | Error error) {
                for (Object monitor : held)
                    dependencies.remove(monitor);
                throw error;
            }
            return held;
        }
    }

    private static void endDependencies(ArrayList<Object> sources) {
        synchronized (dependencyGate) {
            for (Object source : sources)
                dependencies.remove(source);
        }
    }

    // Called only under dependencyGate. Weak identity entries cannot keep a
    // native monitor (or its owning composition) alive after ownership ends.
    private static void observeMonitor(Object monitor) {
        if (!findMonitor(monitor))
            monitors.add(new WeakReference<>(monitor));
    }

    private static boolean findMonitor(Object monitor) {
        boolean found = false;
        for (int i = monitors.size() - 1; i >= 0; i--) {
            Object existing = monitors.get(i).get();
            if (existing == null)
                monitors.remove(i);
            else if (existing == monitor)
                found = true;
        }
        return found;
    }

    static int dependencyCount() {
        synchronized (dependencyGate) {
            return dependencies.size();
        }
    }

    private static boolean registered(CompositionImpl composition, Object token, RecomposeScopeImpl scope)
            throws IllegalAccessException {
        if (scope != null && scope.getValid()
                && composition.getSlotStorage$runtime().ownsRecomposeScope(scope))
            return true;
        if (scope == null) {
            for (Object value : composition.getSlotStorage$runtime().getSlots()) {
                if (value instanceof RememberObserverHolder
                        && ((RememberObserverHolder) value).getWrapped() == token)
                    return true;
            }
        }
        Set<Object> visited = Collections.newSetFromMap(new IdentityHashMap<Object, Boolean>());
        if (contains(changes.get(composition), token, visited)
                || contains(lateChanges.get(composition), token, visited))
            return true;
        Object composer = composition.getComposer$runtime();
        if (composer instanceof androidx.compose.runtime.GapComposer)
            return contains(gapList.get(gapWriter.get(composer)), token, visited);
        if (composer instanceof androidx.compose.runtime.LinkComposer)
            return contains(linkList.get(linkWriter.get(composer)), token, visited);
        throw new IllegalStateException("Unsupported Compose composer in shared-state lifetime query: "
            + composer.getClass().getName());
    }

    private static boolean contains(Object list, Object token, Set<Object> visited)
            throws IllegalAccessException {
        if (list == null || !visited.add(list))
            return false;
        boolean gap = list instanceof androidx.compose.runtime.composer.gapbuffer.changelist.ChangeList;
        Object[] codes;
        Object[] arguments;
        int codeCount;
        int argumentCount;
        if (gap) {
            androidx.compose.runtime.composer.gapbuffer.changelist.Operations operations =
                (androidx.compose.runtime.composer.gapbuffer.changelist.Operations) gapOperations.get(list);
            codes = operations.opCodes;
            arguments = operations.objectArgs;
            codeCount = operations.opCodesSize;
            argumentCount = operations.objectArgsSize;
        } else if (list instanceof androidx.compose.runtime.composer.linkbuffer.changelist.ChangeList) {
            androidx.compose.runtime.composer.linkbuffer.changelist.Operations operations =
                (androidx.compose.runtime.composer.linkbuffer.changelist.Operations) linkOperations.get(list);
            codes = operations.opCodes;
            arguments = operations.objectArgs;
            codeCount = operations.opCodesSize;
            argumentCount = operations.objectArgsSize;
        } else {
            throw new IllegalStateException("Unsupported Compose change list: " + list.getClass().getName());
        }
        int cursor = 0;
        for (int i = 0; i < codeCount; i++) {
            int count;
            String name;
            if (gap) {
                androidx.compose.runtime.composer.gapbuffer.changelist.Operation operation =
                    (androidx.compose.runtime.composer.gapbuffer.changelist.Operation) codes[i];
                count = operation.getObjects();
                name = operation.getName();
            } else {
                androidx.compose.runtime.composer.linkbuffer.changelist.Operation operation =
                    (androidx.compose.runtime.composer.linkbuffer.changelist.Operation) codes[i];
                count = operation.getObjects();
                name = operation.getName();
            }
            if (count < 0 || cursor + count > argumentCount)
                throw new IllegalStateException("Invalid Compose operation arguments in shared-state lifetime query.");
            int holderIndex = -1;
            if (name.equals("Remember") || name.equals("UpdateValue")
                    || (gap && name.equals("UpdateAnchoredValue"))
                    || (!gap && name.equals("UpdateValueRelative")))
                holderIndex = 0;
            else if (name.equals("AppendValue"))
                holderIndex = gap ? 1 : 0;
            if (holderIndex >= 0) {
                if (holderIndex >= count)
                    throw new IllegalStateException("Missing Compose registration argument for " + name);
                Object value = arguments[cursor + holderIndex];
                if (value instanceof RememberObserverHolder
                        && ((RememberObserverHolder) value).getWrapped() == token)
                    return true;
            } else if (name.equals("ApplyChangeList")) {
                if (count == 0)
                    throw new IllegalStateException("Missing nested Compose change list.");
                if (contains(arguments[cursor], token, visited))
                    return true;
            }
            cursor += count;
        }
        if (cursor != argumentCount)
            throw new IllegalStateException("Unexpected Compose argument prefix in shared-state lifetime query.");
        return false;
    }

    private static Field field(Class<?> type, String name) {
        try {
            Field result = type.getDeclaredField(name);
            result.setAccessible(true);
            return result;
        } catch (NoSuchFieldException error) {
            throw new ExceptionInInitializerError(new IllegalStateException(
                "Pinned Compose shared-state lifetime field is unavailable: " + type.getName() + "." + name, error));
        }
    }
}
