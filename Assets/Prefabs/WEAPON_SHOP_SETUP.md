# Система Магазина Оружия - Инструкция по Настройке

## Созданные Скрипты

### Core
- **GameManager.cs** (обновлён) - добавлена система денег с сохранением в PlayerPrefs
- **WeaponInventory.cs** - управление покупкой и экипировкой оружия (2 слота)
- **WeaponLoadout.cs** - синхронизация инвентаря с WeaponHolder

### UI
- **WeaponShop.cs** - главный скрипт магазина (открытие/закрытие, пауза)
- **WeaponShopItem.cs** - элемент списка оружия (покупка, экипировка)
- **WeaponSlotsUI.cs** - HUD слотов оружия (1, 2 с иконками)
- **ShopTrigger.cs** - триггер "Нажмите E чтобы открыть магазин"

---

## Настройка в Unity

### 1. GameManager
1. Найдите GameManager в сцене
2. Установите **Starting Money** (по умолчанию 500)

### 2. WeaponInventory (Singleton)
1. Создайте пустой GameObject: `WeaponInventory`
2. Добавьте компонент `WeaponInventory`
3. Установите **Max Slots** = 2
4. В **All Weapons** перетащите ВСЕ WeaponData ассеты:
   - Pistol.asset
   - AK47.asset
   - SMG.asset
   - Shotgun.asset
   - SniperRifle.asset
   - BurstRifle.asset
   - LMG.asset
   - Revolver.asset
   - DMR.asset
   - MicroSMG.asset
   - HeavyRifle.asset

### 3. UI Магазина
1. Создайте Canvas (если нет)
2. Создайте панель магазина: `ShopPanel`
   - Добавьте компонент `WeaponShop`
   - **Shop Panel** = сама панель
   - **Weapon List Container** = ScrollView > Viewport > Content
   - **Weapon Item Prefab** = префаб элемента списка (создайте ниже)
   - **Money Text** = Text для отображения денег

#### 3.1. Префаб элемента оружия (WeaponItemPrefab)
Создайте префаб с компонентом `WeaponShopItem`:
- **Weapon Icon** (Image) - иконка оружия
- **Weapon Name Text** (Text) - название
- **Damage Text** (Text) - урон оружия (автоматически)
- **Price Text** (Text) - цена или "OWNED" (зелёным)
- **Purchase Button** (Button) - кнопка покупки
- **Equip Slot 1 Button** (Button) - экипировать в слот 1
- **Equip Slot 2 Button** (Button) - экипировать в слот 2
- **Owned Indicator** (GameObject) - индикатор владения (опционально)

### 4. HUD Слотов Оружия
1. На Canvas создайте `WeaponSlotsUI`
2. Добавьте компонент `WeaponSlotsUI`
3. Создайте UI для двух слотов:

**Слот 1:**
- **Slot 1 Icon** (Image) - иконка оружия
- **Slot 1 Number** (Text) - цифра "1"
- **Slot 1 Highlight** (GameObject) - подсветка активного слота
- **Slot 1 Canvas Group** (CanvasGroup) - для прозрачности

**Слот 2:**
- **Slot 2 Icon** (Image) - иконка оружия
- **Slot 2 Number** (Text) - цифра "2"
- **Slot 2 Highlight** (GameObject) - подсветка активного слота
- **Slot 2 Canvas Group** (CanvasGroup) - для прозрачности

**Settings:**
- **Active Alpha** = 1.0 (полная видимость)
- **Inactive Alpha** = 0.5 (полупрозрачный)
- **Active Color** = белый
- **Inactive Color** = серый

### 5. Триггер Магазина
1. Создайте GameObject в сцене: `ShopTrigger`
2. Добавьте компонент `ShopTrigger`
3. **Shop** = ссылка на WeaponShop
4. **Prompt UI** = GameObject с текстом "Press E to open Shop"
5. **Prompt Text** = Text
6. **Trigger Radius** = 3 (радиус активации)

### 6. Игрок
1. Найдите объект игрока с `WeaponHolder`
2. Добавьте компонент `WeaponLoadout`
3. Он автоматически синхронизирует инвентарь с оружием

---

## Управление

### В Игре
- **E** - открыть магазин (рядом с триггером)
- **ESC** - закрыть магазин
- **1, 2** - переключение слотов оружия
- **Колесико мыши** - переключение слотов

### В Магазине
- **Purchase** - купить оружие (если хватает денег)
- **Slot 1 / Slot 2** - экипировать в слот
- Зелёная подсветка = оружие экипировано в этот слот

---

## Система Сохранения (PlayerPrefs)

### Сохраняется:
- **PlayerMoney** - текущие деньги
- **OwnedWeapons** - список купленного оружия (через запятую)
- **EquippedSlot0, EquippedSlot1** - экипированное оружие
- **CurrentSlot** - активный слот

### Сброс прогресса:
```csharp
PlayerPrefs.DeleteAll();
PlayerPrefs.Save();
```

---

## Цены Оружия

Цены теперь хранятся в каждом **WeaponData.asset** в поле `price`:

1. **Pistol** - $0 (бесплатный стартовый)
2. **Revolver** - $400 (дешевое вторичное)
3. **SMG** - $600 (ближний бой)
4. **AK-47** - $800 (универсал)
5. **Burst Rifle** - $900 (точность)
6. **LMG** - $1200 (огневая мощь)
7. **Sniper Rifle** - $1500 (самое дорогое)

**Прогрессия:** От $0 до $1500 для растянутого прогресса

Цены можно изменить в каждом `.asset` файле оружия в секции **Shop > Price**

---

## Добавление Денег (для тестирования)

В GameManager добавьте вызов:
```csharp
GameManager.Instance.AddMoney(1000);
```

Или через консоль Unity:
```csharp
GameManager.Instance.AddMoney(5000);
```

---

## Следующие Шаги (TODO)

- [ ] Прокачка оружия (урон, обойма, скорострельность)
- [ ] Покупка хилок
- [ ] Покупка боеприпасов
- [ ] Награды за волны/убийства
- [ ] Анимации открытия/закрытия магазина
- [ ] Звуки покупки/экипировки
- [ ] Бэкенд вместо PlayerPrefs

---

## Важно!

1. **Pistol должен быть бесплатным** (цена = 0) и автоматически добавляться в owned при первом запуске
2. Все **WeaponData** должны иметь **иконки** (Sprite) для отображения в UI
3. **WeaponInventory** должен быть в сцене ДО старта игры (Singleton)
4. **ShopPanel** должен быть неактивным по умолчанию
