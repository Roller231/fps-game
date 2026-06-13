# Правки для HTML файла билда

## Файл: `Build/index.html`

### 1. Изменить цвет фона загрузочного экрана

**Найти:**
```css
background-color: #222;
```

**Заменить на:**
```css
background-color: #B49375;
```

---

### 2. Исправить размер иконок (unity-logo-dark и unity-logo-light)

Иконки теперь 400x400, но нужно чтобы они отображались в прежнем размере.

**Найти в CSS:**
```css
#unity-logo {
  width: 100px;
  height: 100px;
}
```

**Заменить на:**
```css
#unity-logo {
  width: 100px;
  height: 100px;
  object-fit: contain;
  image-rendering: crisp-edges;
}
```

Или если используется `background-image`:

**Найти:**
```css
background-size: auto;
```

**Заменить на:**
```css
background-size: contain;
background-repeat: no-repeat;
background-position: center;
```

---

### 3. Полный пример CSS для загрузочного экрана

```css
body {
  background-color: #B49375;
  margin: 0;
  padding: 0;
}

#unity-logo {
  width: 100px;
  height: 100px;
  object-fit: contain;
  image-rendering: crisp-edges;
}

#unity-loading-bar {
  flex: 1 1 auto;
  display: flex;
  justify-content: center;
  align-items: center;
}
```

---

## Как применить:

1. Открой `Build/index.html` в текстовом редакторе
2. Найди секцию `<style>` или CSS файл
3. Примени изменения выше
4. Сохрани файл
5. Перезагрузи страницу в браузере (Ctrl+Shift+R для очистки кэша)

## Если иконка всё ещё обрезается:

Добавь в CSS иконки:
```css
#unity-logo {
  width: 100px;
  height: 100px;
  object-fit: contain;
  object-position: center;
}
```

Или используй `padding` вместо `width/height`:
```css
#unity-logo {
  max-width: 100px;
  max-height: 100px;
  padding: 20px;
}
```
