# Деплой на сервер (без домена, только IP)

## Подготовка сервера

Требования:
- Ubuntu/Debian Linux
- Docker + Docker Compose установлены
- Открыты порты: **8000** (backend), **8080** (game)

## Шаги деплоя

### 1. Залить проект на сервер

```bash
# На сервере
cd /opt
git clone <your-repo-url> fps-game
cd fps-game
```

Или через `scp`:
```bash
# С локальной машины
scp -r d:\GitPR\fps-game user@YOUR_SERVER_IP:/opt/fps-game
```

### 2. Настроить .env файл

```bash
cd /opt/fps-game
nano .env
```

Измени `SERVER_HOST` на IP твоего сервера:
```env
SERVER_HOST=123.45.67.89  # <-- твой IP сервера
BACKEND_PORT=8000
WEBGL_PORT=8080
MYSQL_PORT=3307

BACKEND_MYSQL_HOST=mysql
BACKEND_MYSQL_PORT=3306
BACKEND_MYSQL_USER=game_user
BACKEND_MYSQL_PASSWORD=СМЕНИ_НА_СИЛЬНЫЙ_ПАРОЛЬ
BACKEND_MYSQL_DB=game_db
BACKEND_SECRET_KEY=СМЕНИ_НА_СЛУЧАЙНУЮ_СТРОКУ_МИНИМУМ_32_СИМВОЛА
```

### 3. Запустить Docker

```bash
docker-compose up --build -d
```

Проверить статус:
```bash
docker-compose ps
docker-compose logs -f
```

### 4. Готово!

Теперь доступно:
- **Лендинг**: `http://YOUR_IP:8000`
- **Игра**: `http://YOUR_IP:8080` (открывается автоматически через кнопку PLAY)
- **API**: `http://YOUR_IP:8000/api`

## Что происходит автоматически

✅ Unity WebGL автоопределяет backend URL из адреса страницы  
✅ Лендинг подставляет правильные ссылки через Jinja2 шаблоны  
✅ Все контейнеры перезапускаются автоматически (`restart: unless-stopped`)  
✅ Данные (MySQL, аватары) сохраняются в Docker volumes  

## Обновление после изменений

```bash
cd /opt/fps-game
git pull  # или scp новые файлы
docker-compose down
docker-compose up --build -d
```

## Firewall (если включен)

```bash
sudo ufw allow 8000/tcp
sudo ufw allow 8080/tcp
sudo ufw reload
```

## Логи и отладка

```bash
# Все логи
docker-compose logs -f

# Только backend
docker-compose logs -f backend

# Только webgl (nginx)
docker-compose logs -f webgl

# Только mysql
docker-compose logs -f mysql
```

## Остановка

```bash
docker-compose down
```

Данные сохранятся в volumes и восстановятся при следующем запуске.

## Полная очистка (удалить все данные)

```bash
docker-compose down -v
```

⚠️ Это удалит базу данных и загруженные аватары!

---

## Пример: Сервер с IP 123.45.67.89

После деплоя пользователи:
1. Открывают `http://123.45.67.89:8000`
2. Регистрируются/логинятся
3. Нажимают **PLAY**
4. Игра открывается на `http://123.45.67.89:8080/?token=...`
5. Unity автоматически подключается к `http://123.45.67.89:8000/api`

Всё работает без ручной настройки URL!
