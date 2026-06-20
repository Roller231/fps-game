# Backend Integration Guide

## Что сделано

### 1. База данных (MySQL)
Расширена схема БД с тремя таблицами:

- **users** - основная таблица пользователей:
  - `username` - логин (уникальный)
  - `password_hash` - хеш пароля
  - `api_token` - токен для доступа к API
  - `display_name` - отображаемое имя (можно менять)
  - `avatar_url` - URL аватарки (опционально)
  - `balance` - игровая валюта
  - `equipped_weapon` - текущее экипированное оружие
  - `created_at`, `last_login` - даты

- **user_weapons** - купленное оружие и патроны:
  - `user_id` - связь с пользователем
  - `weapon_name` - название оружия
  - `reserve_ammo` - патроны в запасе
  - `magazine_ammo` - патроны в магазине

- **user_stats** - статистика для лидербордов:
  - `user_id` - связь с пользователем
  - `total_kills` - всего убийств
  - `total_money_earned` - всего заработано денег
  - `max_wave` - максимальная волна
  - `max_survival_time` - максимальное время выживания (секунды)
  - `total_playtime` - общее время игры (секунды)

### 2. Backend API (FastAPI)

**Эндпоинты авторизации:**
- `POST /api/register` - регистрация (username, password, confirm_password, display_name)
- `POST /api/login` - вход (username, password)
- `GET /api/token/validate?token=` - проверка токена

**Эндпоинты профиля:**
- `GET /api/profile?token=` - получить профиль (баланс, оружие, статистику)
- `POST /api/profile?token=` - обновить профиль (любые поля)
- `POST /api/profile/password?token=` - сменить пароль

### 3. Лендинг

Добавлены формы:
- **Регистрация** - с полем display_name (опционально)
- **Вход** - логин + пароль
- **Профиль** (появляется после входа):
  - Смена display_name
  - Установка avatar_url
  - Смена пароля

### 4. Unity интеграция

**Новые скрипты:**
- `ProfileService.cs` - синхронизация с бэкендом:
  - Загружает профиль при старте
  - Автосохранение каждые 30 секунд
  - Сохранение при выходе из игры
  - События `OnProfileLoaded`, `OnProfileSaved`

**Обновлённые скрипты:**
- `MoneyManager.cs` - убран PlayerPrefs, добавлен `SetMoney()`
- `WeaponInventory.cs` - убран PlayerPrefs, добавлены `LoadFromProfile()` и `SaveToProfile()`
- `Weapon.cs` - убраны `LoadAmmo()` и `SaveAmmo()`
- `SupplyBox.cs` - убран PlayerPrefs
- `GameManager.cs` - убран PlayerPrefs

**PlayerPrefs полностью удалён** - все данные теперь в БД.

## Как запустить

### 1. Запуск бэкенда (MySQL доступен на 3307)

```bash
cd d:\GitPR\fps-game
docker compose up --build

MySQL внутри контейнера слушает 3306, но на хосте проброшен на 3307, чтобы не конфликтовать с локальной БД.
```

Бэкенд доступен на http://localhost:8000

### 2. Настройка Unity

1. Создай пустой GameObject "**ProfileManager**" в сцене
2. Добавь компонент **ProfileService**
3. Настрой в инспекторе:
   - **Backend Base Url**: `http://localhost:8000`
   - **Auto Save Interval**: `30` (секунды)

4. Убедись что **AuthManager** настроен:
   - **Backend Base Url**: `http://localhost:8000`
   - **Gate Panel** - UI панель блокировки
   - **Disabled Behaviours** - скрипты для отключения (PlayerMovement, WeaponHolder и т.д.)

### 3. Тестирование

1. Открой http://localhost:8000
2. Зарегистрируйся (username + password + display_name)
3. Скопируй полученный токен
4. В Unity установи переменную окружения:
   ```powershell
   setx GAME_LAUNCH_URL "http://localhost:8000/?token=ВАШ_ТОКЕН"
   ```
5. Перезапусти Unity Editor
6. Запусти игру - профиль загрузится автоматически

### 4. WebGL билд

При деплое WebGL билда:
1. Обнови `backendBaseUrl` в AuthManager и ProfileService на публичный URL
2. Пользователь заходит на лендинг → получает токен
3. Кликает на ссылку игры с `?token=...` в URL
4. Игра валидирует токен и загружает профиль

## Что сохраняется

- ✅ Баланс игрока
- ✅ Купленное оружие
- ✅ Экипированное оружие
- ✅ Патроны для каждого оружия
- ✅ Статистика (убийства, заработок, волны, время)
- ✅ Display name и аватарка

## Следующие шаги

1. **Донат-система** - добавить эндпоинты покупки валюты
2. **Лидерборды** - API для топа игроков
3. **Статистика** - отслеживание убийств/волн в Unity и отправка на бэк
4. **Безопасность** - HTTPS, rate limiting, валидация данных
5. **Админка** - управление пользователями и балансом

## Troubleshooting

**Профиль не загружается:**
- Проверь что docker compose запущен
- Проверь консоль Unity на ошибки
- Проверь что токен правильный

**Данные не сохраняются:**
- Убедись что ProfileService.Instance существует
- Проверь автосохранение (каждые 30 сек)
- Проверь логи бэкенда: `docker compose logs backend`

**Ошибка "Token not found":**
- Перелогинься на лендинге
- Скопируй новый токен
- Обнови GAME_LAUNCH_URL
