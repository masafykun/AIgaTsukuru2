# ⚽ Soccer Online

> ブラウザで遊べる 1v1 リアルタイム対戦サッカーゲーム

Rocket League 風の 3D サッカーゲームです。リンクを共有するだけで友達と対戦できます。
Unity WebGL で動作し、スマホ・PC どちらにも対応しています。先に 3 点決めた方が勝ちです。

![Unity](https://img.shields.io/badge/Unity-6000.4.5f1-black?style=flat-square&logo=unity)
![Platform](https://img.shields.io/badge/Platform-WebGL-blue?style=flat-square)
![Language](https://img.shields.io/badge/Language-C%23-239120?style=flat-square&logo=csharp)
![License](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)

🔗 **[今すぐ遊ぶ → https://soccer.1qaz.jp](https://soccer.1qaz.jp)**

---

## 🎮 操作方法

| 操作 | PC (キーボード) | スマホ |
|---|---|---|
| 前進 / 後退 | `W` / `S` | 画面左下のジョイスティック |
| 左右旋回 | `A` / `D` | 画面左下のジョイスティック |

2つのブラウザタブ（または別のデバイス）で同時に開くと対戦できます。

---

## ✨ 特徴

- **リアルタイム対戦** — WebSocket リレーサーバーを介した 1v1 マルチプレイヤー
- **スマホ対応** — 画面左下にバーチャルジョイスティックを表示
- **ゴールエフェクト** — パーティクル・フラッシュライト・スクリーンフラッシュ
- **プロシージャルBGM/SFX** — 8ビット風 BGM・ゴール音・勝利ファンファーレを C# で自動生成
- **勝利画面 & リマッチ** — 3点先取で勝利画面を表示、ホストが REMATCH で即再戦
- **ニックネーム登録** — ゲーム開始時に名前を入力、プレイヤーの頭上に表示
- **Editor SetupWindow** — `Soccer → Open Setup → BUILD SCENE` でシーンを自動構築

---

## 🛠️ 技術スタック

| カテゴリ | 技術 |
|---|---|
| ゲームエンジン | Unity 6 (6000.4.5f1) / Universal 3D |
| レンダリング | Universal Render Pipeline (URP) |
| ネットワーク | NativeWebSocket 1.1.6 (WebGL 対応) |
| サーバー | Node.js WebSocket リレー / PM2 |
| リバースプロキシ | nginx (WSS + HTTPS) |
| SSL | Let's Encrypt (certbot) |
| ホスティング | ConoHa VPS |
| 入力 | Unity InputSystem |

---

## 📁 ディレクトリ構成

```
AIgaTsukuru2/
├── Assets/
│   ├── Editor/
│   │   └── SoccerSetupWindow.cs   # ワンクリックシーン構築
│   └── Scripts/
│       ├── GameManager.cs          # スコア・ゴール・勝利管理
│       ├── GameNetworkManager.cs   # WebSocket 送受信
│       ├── PlayerController.cs     # キーボード・タッチ入力
│       ├── BallSync.cs             # ボール物理同期
│       ├── AudioManager.cs         # プロシージャル音声生成
│       ├── TouchInput.cs           # バーチャルジョイスティック
│       ├── GoalTrigger.cs          # ゴール判定
│       ├── GoalEffect.cs           # パーティクル・ライトエフェクト
│       ├── CameraFollow.cs         # 三人称カメラ追従
│       └── FaceCamera.cs           # ニックネームラベル向き制御
└── Server/
    └── server.js                   # Node.js WebSocket リレーサーバー
```

---

## 🚀 セットアップ

### Unity プロジェクト

```bash
# Unity Hub でクローン先を開く (Unity 6.0 以上)
git clone https://github.com/masafykun/AIgaTsukuru2.git
```

```
1. Unity Hub → プロジェクトを追加 → クローンしたフォルダを選択
2. NativeWebSocket をインストール
   Window → Package Manager → + → Add package from git URL
   → https://github.com/endel/NativeWebSocket.git#upm
3. Soccer → Open Setup → WebSocket URL を設定 → BUILD SCENE
4. File → Build Settings → Web → Build
```

### リレーサーバー (VPS)

```bash
# Node.js サーバーを起動
cd Server
npm install ws
pm2 start server.js --name soccer-server

# nginx で WSS プロキシ設定 (ポート 7779 → wss://soccer-ws.example.com)
```

---

## ライセンス

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

このプロジェクトは **MIT ライセンス** のもとで公開しています。

© 2026 masafykun (https://github.com/masafykun)
