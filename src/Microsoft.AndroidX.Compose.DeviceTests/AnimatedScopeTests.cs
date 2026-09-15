using AndroidX.Compose;
using AndroidX.Compose.Animation;
using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Native default dispatch and managed callback/scope restoration contracts.</summary>
[TestClass]
[DoNotParallelize]
public class AnimatedScopeTests
{
    [TestMethod]
    public void KotlinDefaults_ExcludeBothReceiversAndPreserveEveryExplicitCombination()
    {
        using var recorder = new AnimatedScopeRecorder();
        var explicitEnter = Transitions.ScaleIn(0.4f);
        var explicitExit = Transitions.ScaleOut(0.6f);
        var defaultSpec = AnimationSpecKt.Spring(Spring.DampingRatioNoBouncy, Spring.StiffnessMediumLow, null);
        var defaultEnter = EnterExitTransitionKt.FadeIn(defaultSpec, 0f);
        var defaultExit = EnterExitTransitionKt.FadeOut(defaultSpec, 0f);
        using (RenderContext.PushAnimatedVisibilityScope(recorder))
        {
            for (int supplied = 0; supplied < 8; supplied++)
            {
                EnterTransition? enter = (supplied & 1) != 0 ? explicitEnter : null;
                ExitTransition? exit = (supplied & 2) != 0 ? explicitExit : null;
                string? label = (supplied & 4) != 0 ? "" : null;
                var chain = Modifier.Padding(4).AnimateEnterExit(enter, exit, label).Padding(2);
                _ = chain.Build();
                Assert.AreEqual(supplied + 1, recorder.Calls);
                Assert.AreEqual(enter ?? defaultEnter, recorder.Enter, $"enter, supplied={supplied}");
                Assert.AreEqual(exit ?? defaultExit, recorder.Exit, $"exit, supplied={supplied}");
                Assert.AreEqual(label ?? "animateEnterExit", recorder.Label);
            }
        }
        Assert.IsNull(RenderContext.CurrentAnimatedVisibilityScope);
        Assert.AreEqual(1, (int)AnimateEnterExitDefault.Enter);
        Assert.AreEqual(2, (int)AnimateEnterExitDefault.Exit);
        Assert.AreEqual(4, (int)AnimateEnterExitDefault.Label);
    }

    [TestMethod]
    public void WrongScope_IsDeferredUntilMaterializationAndDoesNotLeak()
    {
        var omitted = Modifier.AnimateEnterExit();
        var explicitValues = Modifier.AnimateEnterExit(Transitions.FadeIn(), Transitions.FadeOut(), "explicit");
        using var recorder = new AnimatedScopeRecorder();
        Modifier[] modifiers = [omitted, explicitValues];
        foreach (var kind in Enum.GetValues<ScopeKind>())
        {
            using var layout = RenderContext.PushScope(IntPtr.Zero, kind);
            foreach (var modifier in modifiers)
            {
                var error = Assert.ThrowsExactly<InvalidOperationException>(() => modifier.Build());
                StringAssert.Contains(error.Message, "AnimatedVisibility or AnimatedContent content scope");
                using (RenderContext.PushAnimatedVisibilityScope(recorder))
                {
                    _ = modifier.Build();
                    Assert.AreSame(recorder, RenderContext.CurrentAnimatedVisibilityScope);
                    Assert.AreEqual(kind, RenderContext.CurrentScopeKind);
                }
                Assert.IsNull(RenderContext.CurrentAnimatedVisibilityScope);
                Assert.ThrowsExactly<InvalidOperationException>(() => modifier.Build());
            }
        }
#pragma warning disable CS8625
        Assert.ThrowsExactly<ArgumentNullException>(() => ModifierExtensions.AnimateEnterExit(null));
#pragma warning restore CS8625
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void LaterCallbacks_RetainLexicalScopeAndRestoreComposerEvenOnException(int arity)
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        IComposer? invocation = null;
        try
        {
            composition.ComposeContent(new ComposableLambda2(c => invocation = c));
            composition.ApplyChanges();
            var composer = invocation ?? throw new InvalidOperationException("Native composer was not captured.");
            using var lexical = new AnimatedScopeRecorder();
            using var foreign = new AnimatedScopeRecorder();
            bool shouldThrow = false;
            int calls = 0;
            void Body(IComposer c)
            {
                Assert.AreSame(composer, c);
                Assert.AreSame(c, ComposableContext.Current);
                Assert.AreSame(lexical, RenderContext.CurrentAnimatedVisibilityScope);
                using (RenderContext.PushAnimatedVisibilityScope(foreign))
                    Assert.AreSame(foreign, RenderContext.CurrentAnimatedVisibilityScope);
                Assert.AreSame(lexical, RenderContext.CurrentAnimatedVisibilityScope);
                calls++;
                if (shouldThrow) throw new InvalidOperationException("deliberate child failure");
            }
            Java.Lang.Object callback;
            using (RenderContext.PushAnimatedVisibilityScope(lexical))
            {
                callback = arity switch
                {
                    2 => new ComposableLambda2((c, changed) =>
                    {
                        Assert.AreEqual(1, changed);
                        Body(c);
                    }),
                    3 => new ComposableLambda3(Body),
                    _ => new ComposableLambda4((_, _, c) => Body(c)),
                };
            }
            using (callback)
            using (var changed = Java.Lang.Integer.ValueOf(1)
                ?? throw new InvalidOperationException("Boxed changed flag unavailable."))
            {
                void Invoke()
                {
                    switch (callback)
                    {
                        case ComposableLambda2 fn: fn.Invoke((Java.Lang.Object)composer, changed); break;
                        case ComposableLambda3 fn: fn.Invoke(null, (Java.Lang.Object)composer, changed); break;
                        case ComposableLambda4 fn: fn.Invoke(null, null, (Java.Lang.Object)composer, changed); break;
                    }
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Java.Lang.JavaSystem.Gc();
                Invoke();
                Assert.IsNull(RenderContext.CurrentAnimatedVisibilityScope);
                Assert.ThrowsExactly<InvalidOperationException>(() => _ = ComposableContext.Current);
                using (RenderContext.PushAnimatedVisibilityScope(foreign))
                using (ComposableContext.Enter(composer))
                {
                    Invoke();
                    Assert.AreSame(foreign, RenderContext.CurrentAnimatedVisibilityScope);
                    shouldThrow = true;
                    StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(Invoke).Message, "deliberate");
                    Assert.AreSame(foreign, RenderContext.CurrentAnimatedVisibilityScope);
                    Assert.AreSame(composer, ComposableContext.Current);
                }
                Assert.AreEqual(3, calls);
            }
        }
        finally { composition.Dispose(); }
        Assert.IsNull(RenderContext.CurrentAnimatedVisibilityScope);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = ComposableContext.Current);
    }

    [TestMethod]
    public void CallbackWithoutLexicalScope_DoesNotBorrowInvocationScope()
    {
        using var applier = new IdentityTestApplier();
        using var recomposer = new Recomposer(Kotlin.Coroutines.EmptyCoroutineContext.Instance
            ?? throw new InvalidOperationException("Empty coroutine context unavailable."));
        var composition = CompositionKt.ControlledComposition(applier, recomposer);
        try
        {
            IComposer? captured = null;
            composition.ComposeContent(new ComposableLambda2(c => captured = c));
            composition.ApplyChanges();
            var composer = captured ?? throw new InvalidOperationException("Native composer was not captured.");
            using var callback = new ComposableLambda2(c =>
            {
                Assert.IsNull(RenderContext.CurrentAnimatedVisibilityScope);
                Assert.ThrowsExactly<InvalidOperationException>(() => Modifier.AnimateEnterExit().Build());
            });
            using var foreign = new AnimatedScopeRecorder();
            using (RenderContext.PushAnimatedVisibilityScope(foreign))
                callback.Invoke((Java.Lang.Object)composer, null);
            Assert.IsNull(RenderContext.CurrentAnimatedVisibilityScope);
        }
        finally { composition.Dispose(); }
    }
}
