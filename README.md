# KS Animator Copy Engine (NDMF)

`com.github.k-stand.ksanimatorcopyengine`が提供するAnimatorController関連オブジェクトの
コピー&ペースト機能を、NDMFのVirtual Animator API(`nadena.dev.ndmf.animator`名前空間)向けに
提供するライブラリです。

## 概要
NDMFビルド中は、実際の`AnimatorController`ではなく`VirtualAnimatorController`/`VirtualLayer`/
`VirtualStateMachine`/`VirtualState`/`VirtualBlendTree`などのVirtualオブジェクトを編集するのが
NDMF推奨の作法です。本パッケージは、コアパッケージが提供するCopy/Paste/Clone機能一式(`ClonePolicy`、
Kindレジストリ、クローン結果検証のプラグイン機構を含む)を、これらのVirtualオブジェクトを対象として
同じ設計思想のまま提供します。パラメーター参照解決の仕組み(後述)は本パッケージが独自に持つものです。

コア(`com.github.k-stand.ksanimatorcopyengine`)とAPI形状・内部アーキテクチャを可能な限り一致させて
移植しているため、コア側のドキュメント・使用例もあわせて参照してください。

VRChat Avatars SDK固有の型(`VRCAvatarParameterDriver`)への対応は標準で同梱されています。
`VRCAvatarParameterDriver`が参照するパラメーターは、パラメーター整合性チェック(`VirtualAnimatorCopyEngineParameterConsistency`)の対象に含まれます。

## インストール
現時点ではVCC(ALCOM)向けのリポジトリ登録・unitypackage配布は行っていません。
`com.github.k-stand.ksanimatorcopyengine.ndmf`に依存する他パッケージ経由で導入するか、
このプロジェクトの`Packages`フォルダを直接参照して利用してください。

## 使用方法
```csharp
// Layer単位でコピーして、別のVirtualAnimatorControllerへペースト
VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorCopyEngine.Copy(sourceLayer, sourceController);
VirtualAnimatorCopyEngine.PasteLayers(clipSet, destController);

// State/Transition/BlendTreeなど任意のオブジェクトをコピーして、Layer内にペースト
VirtualAnimatorCopyClipSet objClipSet = VirtualAnimatorCopyEngine.Copy(sourceState, sourceLayer);
VirtualAnimatorCopyEngine.PasteIntoLayer(objClipSet, destLayer, cloneContext);

// 同じコピー内容をNDMFのCloneContext経由でクローンして、独立したコピーとして貼り付ける
VirtualAnimatorCopyClipSet cloned = objClipSet.Clone(cloneContext, out Dictionary<object, object> clonedMap);
VirtualAnimatorCopyEngine.PasteIntoStateMachine(cloned, destStateMachine, cloneContext);
```

参照先オブジェクトのクローン方針(`ClonePolicy`)は、対応する`IVirtualAnimatorCopyObjectKind`実装の
登録内容に従います。コア同様、`VirtualAnimatorCloner.ValidateRegistrations()`で未登録の型を事前に検出できます。

## 依存関係
- `com.github.k-stand.ksanimatorcopyengine`
- `nadena.dev.ndmf`
- `com.vrchat.avatars`

## License
[MIT License](https://github.com/k-stand/KSAnimatorClipboard/blob/main/LICENSE.txt)

## 更新履歴
### [2026-07-28] 0.3.0
- パッケージを`com.github.k-stand.ksanimatorclipboard.ndmf`から`com.github.k-stand.ksanimatorcopyengine.ndmf`へ改名(破壊的変更)。コアパッケージ(`com.github.k-stand.ksanimatorcopyengine`)の改名に伴うものです
- エントリーポイントクラス`VirtualAnimatorClipboard`を`VirtualAnimatorCopyEngine`に、`VirtualAnimatorClipboardParameterConsistency`を`VirtualAnimatorCopyEngineParameterConsistency`にリネーム(破壊的変更)
- namespace・asmdef名を`com.github.k_stand.ksanimatorclipboard.ndmf.*`から`com.github.k_stand.ksanimatorcopyengine.ndmf.*`に変更(破壊的変更)

### [2026-07-26] 0.2.0
- `com.github.k-stand.ksanimatorclipboard.ndmf.vrchatavatars`パッケージを廃止し、VRChatAvatars対応(`VRCAvatarParameterDriver`)を`Editor/VRChatAvatars`モジュールとして標準同梱(破壊的変更、`com.vrchat.avatars`が新たに必須の依存関係になります)
- `IVirtualParameterReferenceResolver`/`VirtualParameterReferenceResolverRegistry`を`public`から`internal`に変更(外部パッケージからの拡張は不可に。破壊的変更)

### [2026-07-21] 0.1.0
- 初版リリース
- コアパッケージ(`com.github.k-stand.ksanimatorclipboard`)のCopy/Paste/ClonePolicy/Kindレジストリ機構を
  NDMF Virtual Animator API向けに移植
- パラメーター整合性チェック(VirtualAnimatorClipboardParameterConsistency)向けの参照解決プラグイン機構
  (IVirtualParameterReferenceResolver)を追加
- StateMachineBehaviourのクローン結果を検証するプラグイン機構
  (IVirtualStateMachineBehaviourCloneResultValidator)を追加
