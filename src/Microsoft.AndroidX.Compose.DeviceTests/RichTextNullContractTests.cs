using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies required rich-text inputs reject null consistently.</summary>
[TestClass]
public class RichTextNullContractTests
{
    [TestMethod]
    public void AnnotatedString_RejectsNullText()
    {
#pragma warning disable CS8600, CS8625
        var exception = Assert.ThrowsExactly<ArgumentNullException>(
            () => new AnnotatedString((string)null));
#pragma warning restore CS8600, CS8625

        Assert.AreEqual("text", exception.ParamName);
    }

    [TestMethod]
    public void AnnotatedStringBuilder_RejectsNullAppendText()
    {
        var builder = new AnnotatedStringBuilder();

#pragma warning disable CS8625
        var exception = Assert.ThrowsExactly<ArgumentNullException>(
            () => builder.Append(null));
#pragma warning restore CS8625

        Assert.AreEqual("text", exception.ParamName);
    }

    [TestMethod]
    public void AnnotatedStringBuilder_RejectsNullStringAnnotationValues()
    {
        var builder = new AnnotatedStringBuilder();

#pragma warning disable CS8625
        var tagException = Assert.ThrowsExactly<ArgumentNullException>(
            () => builder.AddStringAnnotation(null, "value", 0, 0));
        var annotationException = Assert.ThrowsExactly<ArgumentNullException>(
            () => builder.AddStringAnnotation("tag", null, 0, 0));
#pragma warning restore CS8625

        Assert.AreEqual("tag", tagException.ParamName);
        Assert.AreEqual("annotation", annotationException.ParamName);
    }

    [TestMethod]
    public void LinkAnnotation_RejectsNullRequiredValues()
    {
#pragma warning disable CS8625
        var urlException = Assert.ThrowsExactly<ArgumentNullException>(
            () => LinkAnnotation.Url(null));
        var tagException = Assert.ThrowsExactly<ArgumentNullException>(
            () => LinkAnnotation.Clickable(null, static _ => { }));
        var callbackException = Assert.ThrowsExactly<ArgumentNullException>(
            () => LinkAnnotation.Clickable("tag", null));
#pragma warning restore CS8625

        Assert.AreEqual("url", urlException.ParamName);
        Assert.AreEqual("tag", tagException.ParamName);
        Assert.AreEqual("onClick", callbackException.ParamName);
    }

    [TestMethod]
    public void LinkClickListener_RejectsNullRequiredValues()
    {
#pragma warning disable CS8625
        var tagException = Assert.ThrowsExactly<ArgumentNullException>(
            () => new LinkClickListener(null, static _ => { }));
        var callbackException = Assert.ThrowsExactly<ArgumentNullException>(
            () => new LinkClickListener("tag", null));
#pragma warning restore CS8625

        Assert.AreEqual("tag", tagException.ParamName);
        Assert.AreEqual("onClick", callbackException.ParamName);
    }
}
