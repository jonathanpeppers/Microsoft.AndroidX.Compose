package composenet.compose;

import java.lang.reflect.Field;
import java.util.Collections;
import java.util.IdentityHashMap;
import java.util.Set;

import androidx.compose.runtime.CompositionImpl;
import androidx.compose.runtime.PausedComposition;
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

    private SharedStateLifetime() { }

    static PausedComposition pausedOrigin(CompositionImpl composition) {
        try {
            synchronized (lock.get(composition)) {
                return (PausedComposition) pendingPausedComposition.get(composition);
            }
        } catch (IllegalAccessException error) {
            throw new IllegalStateException("Cannot capture pinned Compose shared-state origin.", error);
        }
    }

    static boolean isLive(CompositionImpl composition, Object token, RecomposeScopeImpl scope,
            PausedComposition registrationOrigin, PausedComposition ownershipOrigin) {
        try {
            synchronized (lock.get(composition)) {
                // Paused resume installs slots before final apply. Cancellation can
                // discard their registration set and then fail during abandonment.
                // Qualify every membership result with the captured origins, not
                // whichever unrelated transaction the composition currently owns.
                return !composition.isDisposed() && registered(composition, token, scope)
                    && (registrationOrigin == null || !registrationOrigin.isCancelled())
                    && (ownershipOrigin == null || !ownershipOrigin.isCancelled());
            }
        } catch (IllegalAccessException error) {
            throw new IllegalStateException("Cannot inspect pinned Compose shared-state lifetime.", error);
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
