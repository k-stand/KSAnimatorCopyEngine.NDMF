# KS Animator Copy Engine (NDMF)

`com.github.k-stand.ksanimatorcopyengine`が提供するAnimatorController関連オブジェクトの
コピー&ペースト機能を、NDMFのVirtual Animator API(`nadena.dev.ndmf.animator`名前空間)向けに
提供するライブラリです。

## 概要
NDMFビルド中は、実際の`AnimatorController`ではなく`VirtualAnimatorController`/`VirtualLayer`/
`VirtualStateMachine`/`VirtualState`/`VirtualBlendTree`などのVirtualオブジェクトを編集するのが
NDMF推奨の作法です。本パッケージは、コアパッケージ(`com.github.k-stand.ksanimatorcopyengine`)と
同じ設計思想でCopy/Paste/Clone機能一式(`ClonePolicy`、Kindレジストリ、クローン結果検証のプラグイン
機構を含む)を、これらのVirtualオブジェクトを対象に独自実装として提供します。コアパッケージへの
依存は無く、パラメーター参照解決の仕組み(後述)を含め本パッケージ単体で完結しています。

API形状・内部アーキテクチャを可能な限りコアパッケージと一致させて移植しているため、コア側の
ドキュメント・使用例もあわせて参照してください。

VRChat Avatars SDK固有の型(`VRCAvatarParameterDriver`)への対応は標準で同梱されています。
`VRCAvatarParameterDriver`が参照するパラメーターは、パラメーター整合性チェック(`VirtualAnimatorCopyEngineParameterConsistency`)の対象に含まれます。

## インストール
### VCC(ALCOM)を利用する方法
1. https://k-stand.github.io/vpm-repos/ の`Add to VCC`を押してVCCにリポジトリを追加します。
2. 導入したいプロジェクトに`KS Animator Copy Engine`をインストールしてください。

### VPAI unitypackageでVCCにインストールする方法
1. 以下から任意のバージョンの`com.github.k-stand.ksanimatorcopyengine.ndmf.X.x.x-installer.unitypackage`をダウンロードして、導入したいプロジェクトにインポートしてください。

0.x.x : [com.github.k-stand.ksanimatorcopyengine.ndmf.0.x.x-installer.unitypackage](https://github.com/k-stand/KSAnimatorCopyEngine.NDMF/releases/download/0.3.1/com.github.k-stand.ksanimatorcopyengine.ndmf.0.x.x-installer.unitypackage)

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

// State/Transitionなど種類の異なるオブジェクトを混在させてまとめてコピーすることもできる
VirtualAnimatorCopyClipSet mixedClipSet = VirtualAnimatorCopyEngine.Copy(
    new object[] { sourceState, sourceTransition }, sourceLayer);
```

参照先オブジェクトのクローン方針(`ClonePolicy`)は、対応する`IVirtualAnimatorCopyObjectKind`実装の
登録内容に従いますが、`VirtualAnimatorCopyClipSet.Clone(VirtualAnimatorCloner)`を使うと、オブジェクトごとに
ClonePolicyを個別指定できます。

```csharp
// ClonePolicyを個別に指定したい場合は、VirtualAnimatorClonerを明示的に生成し、
// 対象オブジェクトごとにSetClonePolicy/SetRangeClonePolicyで方針を設定してから、
// Clone(cloner)経由でクローンする
VirtualAnimatorCloner cloner = new(cloneContext) { DefaultPolicy = VirtualAnimatorCloner.ClonePolicy.KeepReference };
cloner.SetClonePolicy(sourceBlendTree, VirtualAnimatorCloner.ClonePolicy.Clone);
VirtualAnimatorCopyClipSet customCloned = objClipSet.Clone(cloner);
```

未登録の型をクローンしようとした場合、`(new VirtualAnimatorCloner(cloneContext)).ValidateRegistrations(targets)`で事前に検出できます。

## 依存関係
- `nadena.dev.ndmf`
- `com.vrchat.avatars`

コアパッケージ(`com.github.k-stand.ksanimatorcopyengine`)への依存はありません(API形状・内部アーキテクチャを
移植しているのみで、実行時の参照は持ちません)。

## License
[MIT License](https://github.com/k-stand/KSAnimatorCopyEngine.NDMF/blob/main/LICENSE.txt)

## 更新履歴
### [2026-07-28] 0.3.1
- READMEの依存関係の記載を修正。コアパッケージ(`com.github.k-stand.ksanimatorcopyengine`)への実行時依存はなく(API形状・内部アーキテクチャを移植しているのみ)、`vpmDependencies`とasmdef参照にのみ残っていた不要な参照だったため削除しました
- READMEの概要からClonePolicy・Kindレジストリ等の拡張性に関する記述を整理し、使用方法にClonePolicyの個別指定(`VirtualAnimatorCloner`を明示的に使う方法)と、種類の異なるオブジェクトを混在させたコピーの例を追加

### [2026-07-28] 0.3.0
- パッケージを`com.github.k-stand.ksanimatorclipboard.ndmf`から`com.github.k-stand.ksanimatorcopyengine.ndmf`へ改名(破壊的変更)。コアパッケージ(`com.github.k-stand.ksanimatorcopyengine`)の改名に伴うものです
- エントリーポイントクラス`VirtualAnimatorClipboard`を`VirtualAnimatorCopyEngine`に、`VirtualAnimatorClipboardParameterConsistency`を`VirtualAnimatorCopyEngineParameterConsistency`にリネーム(破壊的変更)
- namespace・asmdef名を`com.github.k_stand.ksanimatorclipboard.ndmf.*`から`com.github.k_stand.ksanimatorcopyengine.ndmf.*`に変更(破壊的変更)
- GitHubにて公開

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
