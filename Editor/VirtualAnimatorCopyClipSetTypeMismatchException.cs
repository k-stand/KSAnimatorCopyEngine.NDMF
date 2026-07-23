using System;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor
{
    /// <summary>
    /// VirtualAnimatorCopyClipSetの実際のTypeが要求された型と一致しない場合に、Try接頭辞を持たないPaste系メソッドから送出される例外です。
    /// </summary>
    public sealed class VirtualAnimatorCopyClipSetTypeMismatchException : InvalidOperationException
    {
        public VirtualAnimatorCopyClipSetTypeMismatchException(string message) : base(message) { }
    }
}
