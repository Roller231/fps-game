# Настройка системы денег и наград

## Быстрая настройка (5 минут)

### 1. Создай MoneyManager
1. В сцене создай пустой GameObject: `Create Empty` → назови "MoneyManager"
2. Добавь компонент `MoneyManager` (скрипт уже создан)
3. Этот объект будет DontDestroyOnLoad

### 2. Настрой UI отображение денег (Legacy Text)
1. Найди свой Canvas с HUD
2. Создай Legacy Text: `Right Click → UI → Legacy → Text`
3. Назови его "MoneyText"
4. Расположи в углу экрана (например, правый верхний)
5. Добавь компонент `MoneyUI` на этот текст
6. В инспекторе `MoneyUI`:
   - `Money Text` → перетащи Legacy Text (не TextMeshPro!)
   - `Money Prefix` → оставь "$" или измени на "💰"

### 3. Настрой анимацию заработка (Legacy Animation)
Для floating text при убийстве врагов с Legacy Animation:

1. Создай новый Canvas: `Create → UI → Canvas`
2. Назови "WorldCanvas"
3. В инспекторе Canvas:
   - `Render Mode` → **World Space**
   - `Event Camera` → перетащи Main Camera
   - `Sorting Layer` → Default
   - `Order in Layer` → 100
4. Создай префаб для floating text:
   - Создай пустой GameObject на WorldCanvas
   - Добавь `Text` (Legacy)
   - Добавь `Animation` компонент
   - Создай анимацию: Window → Animation → Create New Clip
   - Анимируй: position.y (подъём) + color.a (исчезновение)
   - В Animation component: `Play Automatically` = false
5. В компоненте `MoneyUI`:
   - `Floating Text Prefab` → перетащи созданный префаб
   - `World Canvas` → перетащи WorldCanvas

### 4. Проверь настройки
- Запусти игру
- Убей врага
- Должна появиться анимация "+50" (или другая сумма)
- В углу экрана должен обновиться счётчик денег

## Что уже готово:

✅ **Скрипты созданы:**
- `MoneyManager.cs` - управление деньгами (PlayerPrefs)
- `MoneyUI.cs` - отображение через Legacy Text + Legacy Animation
- `EnemyData.cs` - добавлено поле `moneyReward`
- `EnemyAI.cs` - выдаёт деньги при смерти
- `WeaponInventory.cs` - использует MoneyManager для покупок
- `WeaponShop.cs` - обновлён для MoneyManager

✅ **Цены настроены (учитываем сохранение между сессиями):**
- Все оружия имеют цены (от 0 до 120000$)
- Все враги дают награды (от 8 до 120$)
- Награды уменьшены в 5 раз для увеличения гринда

✅ **Баланс рассчитан:**
- Экономика настроена на 200+ волн с сохранением оружия
- Создаёт желание донатить уже на волне 5-10
- Подробности в `ECONOMY_BALANCE.md`

## Тестирование

### Добавить денег для теста:
```csharp
// В консоли Unity или через кнопку UI
MoneyManager.Instance.AddMoney(10000);
```

### Сбросить деньги:
```csharp
MoneyManager.Instance.ResetMoney();
```

### Проверить баланс:
```csharp
Debug.Log("Money: " + MoneyManager.Instance.CurrentMoney);
```

## Дополнительные настройки

### Изменить награду за врага:
1. Найди ScriptableObject врага в `Assets/Prefabs/Enemy/`
2. Открой в инспекторе
3. Найди секцию **Rewards**
4. Измени `Money Reward`

### Изменить цену оружия:
1. Найди ScriptableObject оружия в `Assets/Prefabs/Weapons/player_hand_weapon/`
2. Открой в инспекторе
3. Найди секцию **Shop**
4. Измени `Price`

### Настроить стартовые деньги:
В `MoneyManager.cs` измени строку:
```csharp
currentMoney = PlayerPrefs.GetInt("PlayerMoney", 0); // 0 = стартовая сумма
```

## Интеграция доната (будущее)

Когда будешь добавлять донат, используй:
```csharp
// При успешной покупке доната
MoneyManager.Instance.AddMoney(amount);
```

Примеры пакетов:
- Starter Pack ($0.99) → 5,000$
- Weapon Pack ($2.99) → 15,000$
- Premium Pack ($4.99) → 30,000$
- Mega Pack ($9.99) → 75,000$
