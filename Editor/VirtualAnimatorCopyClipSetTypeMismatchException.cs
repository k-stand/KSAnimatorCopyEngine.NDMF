using System;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor
{
    /// <summary>
    /// VirtualAnimatorCopyClipSetの実際のTypeが要求された型と一致しない場合に、Try接頭辞を持たないPaste系メソッドから送出される例外です。
    /// </summary>
    public sealed class VirtualAnimatorCopyClipSetTypeMismatchException : InvalidOperationException
    {
        /// <summary>
        /// VirtualAnimatorCopyClipSetTypeMismatchExceptionの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="message">例外の内容を説明するメッセージ。</param>
        public VirtualAnimatorCopyClipSetTypeMismatchException(string message) : base(message) { }
    }
}
