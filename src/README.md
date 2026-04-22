# src/ — Unity source of «Клоака печали»

Unity 6 LTS (6000.0.72f1), WebGL build target.

Исходный код художественного произведения «Клоака печали». Полный
контекст проекта, манифест автора и лицензия — в корне репозитория.

## Сборка

1. Открыть `src/` в Unity 6 LTS.
2. File → Build Settings → Platform: WebGL → Build.
3. Выходную папку положить в корень репо как `game/` (см. root-level
   GitHub Pages публикует её по пути `/game/`).

## Архитектура

Сцена собирается из кода — нет `.prefab`, нет вручную настроенной
сцены. Точка входа: `Assets/Scripts/GameBootstrap.cs`.

Весь контент (враги, уровни, оружие) — статические массивы в
`Assets/Scripts/Content/Catalog.cs`.

HUD — IMGUI через `Assets/Scripts/HudFactory.cs`.

3D-модели — см. `tools/tripo/` (Tripo3D pipeline).

## Лицензия

CC BY-NC-SA 4.0 — см. `../LICENSE` в корне репозитория.
