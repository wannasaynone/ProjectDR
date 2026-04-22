// AssemblyInfo.cs — Game Assembly 的程式集資訊。
// 允許 Game.Tests 存取 Game Assembly 的 internal 成員，
// 以便在 EditMode 單元測試中直接設定 VillageContext 的 internal set 欄位
// （如 AffinityReadOnly、StorageReadOnly、BackpackReadOnly 等）。

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Game.Tests")]
