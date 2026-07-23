# KS Animator Clipboard (NDMF)

`com.github.k-stand.ksanimatorclipboard`が提供するAnimatorController関連オブジェクトの
コピー&ペースト機能を、NDMFのVirtual Animator API(`nadena.dev.ndmf.animator`名前空間)向けに
提供するライブラリです。

## 概要
NDMFビルド中は、実際の`AnimatorController`ではなく`VirtualAnimatorController`/`VirtualLayer`/
`VirtualStateMachine`/`VirtualState`/`VirtualBlendTree`などのVirtualオブジェクトを編集するのが
NDMF推奨の作法です。本パッケージは、コアパッケージが提供するCopy/Paste/Clone機能一式(`ClonePolicy`、
Kindレジストリ、参照解決・クローン結果検証のプラグイン機構を含む)を、これらのVirtualオブジェクトを
対象として同じ設計思想のまま提供します。

コア(`com.github.k-stand.ksanimatorclipboard`)とAPI形状・内部アーキテクチャを可能な限り一致させて
移植しているため、コア側のドキュメント・使用例もあわせて参照してください。

VRChat Avatars SDK固有の型(`VRCAvatarParameterDriver`など)への対応は、本パッケージではなく
`com.github.k-stand.ksanimatorclipboard.ndmf.vrchatavatars`が提供します。

## インストール
現時点ではVCC(ALCOM)向けのリポジトリ登録・unitypackage配布は行っていません。
`com.github.k-stand.ksanimatorclipboard.ndmf`に依存する他パッケージ経由で導入するか、
このプロジェクトの`Packages`フォルダを直接参照して利用してください。

## 使用方法
```csharp
// Layer単位でコピーして、別のVirtualAnimatorControllerへペースト
VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy(sourceLayer, sourceController);
VirtualAnimatorClipboard.PasteLayers(clipSet, destController);

// State/Transition/BlendTreeなど任意のオブジェクトをコピーして、Layer内にペースト
VirtualAnimatorCopyClipSet objClipSet = VirtualAnimatorClipboard.Copy(sourceState, sourceLayer);
VirtualAnimatorClipboard.PasteIntoLayer(objClipSet, destLayer, cloneContext);

// 同じコピー内容をNDMFのCloneContext経由でクローンして、独立したコピーとして貼り付ける
VirtualAnimatorCopyClipSet cloned = objClipSet.Clone(cloneContext, out Dictionary<object, object> clonedMap);
VirtualAnimatorClipboard.PasteIntoStateMachine(cloned, destStateMachine, cloneContext);
```

参照先オブジェクトのクローン方針(`ClonePolicy`)は、対応する`IVirtualAnimatorCopyObjectKind`実装の
登録内容に従います。コア同様、`VirtualAnimatorCloner.ValidateRegistrations()`で未登録の型を事前に検出できます。

## 依存関係
- `com.github.k-stand.ksanimatorclipboard`
- `nadena.dev.ndmf`

## License
[MIT License](https://github.com/k-stand/KSAnimatorClipboard/blob/main/LICENSE.txt)

## 更新履歴
### [2026-07-21] 0.1.0
- 初版リリース
- コアパッケージ(`com.github.k-stand.ksanimatorclipboard`)のCopy/Paste/ClonePolicy/Kindレジストリ機構を
  NDMF Virtual Animator API向けに移植
- パラメーター整合性チェック(VirtualAnimatorClipboardParameterConsistency)向けの参照解決プラグイン機構
  (IVirtualParameterReferenceResolver)を追加
- StateMachineBehaviourのクローン結果を検証するプラグイン機構
  (IVirtualStateMachineBehaviourCloneResultValidator)を追加
