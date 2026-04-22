# tools/tripo — генерация 3D-моделей для Space Shooter

Скрипты и промпты, которые превращают текстовые описания HR/рекрутинг-монстров в
3D-модели и кладут их в `Assets/Resources/` так, чтобы Unity подхватил их на
следующей сборке.

## Файлы

| Файл | Назначение |
|---|---|
| `prompts.json` | Машинно-читаемый список всех моделей: slug, путь в проекте, EN/RU-промпт. Единый источник правды. |
| `generate.py`  | Python-скрипт для пакетной генерации через Tripo3D API. |
| `prompts.md`   | **Fallback для ручного пути**: те же промпты, оформленные под копипаст в веб-интерфейс tripo3d.ai. |

## Путь A — через API

1. Зарегистрироваться на https://tripo3d.ai, создать API-ключ.
2. `export TRIPO_API_KEY=<ключ>`
3. Из корня репозитория:
   ```bash
   python tools/tripo/generate.py --list               # что уже есть, что не хватает
   python tools/tripo/generate.py                      # сгенерить всё недостающее
   python tools/tripo/generate.py --only boss_interviewer
   python tools/tripo/generate.py --only hh_drone --force
   ```
4. Проверить, что файлы легли в `Assets/Resources/Enemies/` и `Assets/Resources/Bosses/`.
5. Коммит + пуш → GitHub Actions соберёт WebGL с новыми моделями.

Скрипт пропускает уже существующие файлы. Для регенерации — `--force`.

## Путь B — вручную через веб (если квота кончилась)

Открыть [prompts.md](prompts.md), копировать промпт, скачать GLB, положить в
указанный `target_path`, закоммитить. Игра работает даже без моделей — есть
примитив-фолбек нужного цвета.

## Как Unity подхватывает модели

`EnemySpawner.TryLoadModel()` вызывает `Resources.Load<GameObject>(resourcePath)`.
Путь ресурса (без расширения и без префикса `Assets/Resources/`) задан в
`Assets/Scripts/Content/Catalog.cs` в поле `EnemyDefinition.resourcePath`:

```
resourcePath = "Enemies/hh_drone"   →  Assets/Resources/Enemies/hh_drone.glb
```

GLB-файлы импортируются Unity автоматически благодаря пакету
`com.unity.cloud.gltfast`, подключённому в `Packages/manifest.json`.
Импортированный GLB становится обычным Unity-префабом, а спаунер
перекрашивает все его MeshRenderer'ы в палитру текущего уровня, чтобы
внешний вид согласовался с игровым synthwave-стилем.

## Добавить нового монстра

1. Добавить запись в `prompts.json` (`slug`, `target_path`, оба промпта).
2. Добавить пункт в `prompts.md` (тот же промпт, человекочитаемо).
3. Добавить `EnemyFlavor` в `Assets/Scripts/Content/Catalog.cs` и новую
   запись в `Catalog.Enemies`, указав `resourcePath = "Enemies/<slug>"`.
4. Прописать flavor в `roster` одного из уровней в `Catalog.Levels`.
5. Сгенерировать модель (путь A или B).
