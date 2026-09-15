# TemplateEditor — установка

Требования: Windows, .NET SDK 8 или новее и Node.js 24 с npm

## Настройка

1. Скопируйте содержимое `data` в `C:\TemplateEditorData` или другую папку для рабочих данных.
2. В `backend/settings.xml` укажите абсолютные пути: `path_to_prefs`, `path_to_templates`, `path_to_pictures`, `path_to_old_pictures` к `preferences.xml`, `Broadcasts`, `Uploads` и `old_png` соответственно.
3. В рабочем `preferences.xml` и XML внутри `Broadcasts` замените `C:\TemplateEditorData` на свой путь. Маски `{channel}` и `{preset}` трогать не надо. Атрибут `animation folder` должен указывать на конкретный PNG или ZIP.
4. В `settings.xml` задайте `listen_url` — адрес API и `frontend_origin` — адрес страницы фронтенда. В `frontend/public/config.json` укажите тот же доступный браузеру адрес API в `apiBaseUrl`, без `/api/image`.

## Подготовка и запуск из исходников
В корне проекта:

```cmd
dotnet restore backend\TemplateEditor.csproj
cd frontend
npm ci
cd ..
start.bat
```

## Сборка
В корне проекта:

```cmd
dotnet publish backend\TemplateEditor.csproj -c Release -o artifacts\backend
cd frontend
npm run build
```

Настройте `settings.xml` рядом с EXE и запустите `TemplateEditor.exe`; рабочая папка запуска значения не имеет.

По умолчанию API — `http://localhost:5087`, а фронт — `http://localhost:8080`.

Адрес API можно менять без пересборки в `settings.xml`(перезапустить бэк) И!!! `public\config.json` (обновить страницу).

Адрес фронта изменяется в `settings.xml`(перезапустить бэк) И!!! В `frontend\.env.development`(перебилд).