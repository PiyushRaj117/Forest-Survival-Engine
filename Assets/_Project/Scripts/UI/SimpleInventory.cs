using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestSurvival.Inventory
{
    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public int count;
        public Sprite icon;
        public string category; // "weapon", "consumable", "material"
    }

    public class SimpleInventory : MonoBehaviour
    {
        public static SimpleInventory Instance { get; private set; }

        [Header("Inventory Data")]
        [SerializeField] private int maxSlots = 8;
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip itemPickupSound;
        [SerializeField] private AudioClip eatDrinkSound;

        // UI State
        private bool inventoryOpen = false;
        private GameObject inventoryPanel;      // Full inventory popup
        private GameObject quickbarRoot;        // Always-visible hotbar at bottom
        private List<GameObject> fullSlotObjs   = new List<GameObject>();
        private List<GameObject> quickSlotObjs  = new List<GameObject>();

        private Font uiFont;

        // Item category to symbol mapping
        private static readonly Dictionary<string, string> ItemSymbols = new Dictionary<string, string>
        {
            { "Sword",        "\u2694" },   // ⚔ swords
            { "Water Bottle", "\uD83D\uDCA7" }, // 💧
            { "Berries",      "\uD83C\uDF53" }, // 🍓
            { "Wood",         "\uD83E\uDEB5" }, // 🪵
            { "Stone",        "\u26AA" },   // ⚪
            { "Torch",        "\uD83D\uDD25" }, // 🔥
            { "Bandage",      "\u2764" },   // ❤
            { "Bow",          "\uD83C\uDFF9" }, // 🏹
            { "Arrow",        "\u2192" },   // →
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Starter survival gear
            AddItemSilent("Sword",        1, "weapon");
            AddItemSilent("Water Bottle", 2, "consumable");
            AddItemSilent("Berries",      5, "consumable");
            AddItemSilent("Wood",         3, "material");
        }

        private void Start()
        {
            BuildQuickBar();
            BuildInventoryPanel();
            RefreshAll();
        }

        // ─────────────────────── Item Management ───────────────────────

        private void AddItemSilent(string name, int amount, string category = "material")
        {
            InventoryItem existing = items.Find(x => x.itemName == name);
            if (existing != null) { existing.count += amount; return; }
            if (items.Count >= maxSlots) return;
            items.Add(new InventoryItem { itemName = name, count = amount, category = category });
        }

        public bool AddItem(string name, int amount, string category = "material")
        {
            InventoryItem existing = items.Find(x => x.itemName == name);
            if (existing != null)
            {
                existing.count += amount;
            }
            else
            {
                if (items.Count >= maxSlots) return false;
                items.Add(new InventoryItem { itemName = name, count = amount, category = category });
            }

            if (audioSource != null && itemPickupSound != null)
                audioSource.PlayOneShot(itemPickupSound, 0.8f);

            RefreshAll();
            return true;
        }

        public bool UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= items.Count) return false;

            InventoryItem item = items[slotIndex];
            PlayerVitals vitals = FindAnyObjectByType<PlayerVitals>();
            bool consumed = false;

            switch (item.itemName)
            {
                case "Berries":
                    if (vitals != null) { vitals.Eat(22f); vitals.Heal(10f); consumed = true; }
                    break;
                case "Water Bottle":
                    if (vitals != null) { vitals.Drink(40f); consumed = true; }
                    break;
                case "Bandage":
                    if (vitals != null) { vitals.Heal(30f); consumed = true; }
                    break;
                case "Sword":
                case "Bow":
                    CustomPlayerController player = FindAnyObjectByType<CustomPlayerController>();
                    if (player != null) player.EnsureWeaponEquipped();
                    consumed = false;
                    break;
            }

            if (consumed)
            {
                if (audioSource != null && eatDrinkSound != null)
                    audioSource.PlayOneShot(eatDrinkSound, 0.85f);

                item.count--;
                if (item.count <= 0)
                    items.RemoveAt(slotIndex);
            }

            RefreshAll();
            return true;
        }

        // ─────────────────────── Input Handling ───────────────────────

        private void Update()
        {
            // Toggle full inventory with Tab
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                inventoryOpen = !inventoryOpen;
                if (inventoryPanel != null) inventoryPanel.SetActive(inventoryOpen);

                // Manage cursor
                if (inventoryOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else if (Time.timeScale > 0f) // Only lock if not paused
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            // Close on Escape
            if (Input.GetKeyDown(KeyCode.Escape) && inventoryOpen)
            {
                inventoryOpen = false;
                if (inventoryPanel != null) inventoryPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Hotkeys 1-8 for quick-use
            for (int i = 0; i < Mathf.Min(8, maxSlots); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    UseItem(i);
                    FlashQuickSlot(i);
                }
            }
        }

        // ─────────────────────── Quick Bar (always visible) ───────────────────────

        private void BuildQuickBar()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Remove old if any
            Transform prev = canvas.transform.Find("QuickBar");
            if (prev != null) Destroy(prev.gameObject);

            quickbarRoot = new GameObject("QuickBar", typeof(RectTransform));
            quickbarRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = quickbarRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot     = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 20f);

            int visibleSlots = Mathf.Min(maxSlots, 8);
            float slotSize = 68f;
            float spacing  = 6f;
            float totalW   = visibleSlots * slotSize + (visibleSlots - 1) * spacing + 16f;
            rootRect.sizeDelta = new Vector2(totalW, slotSize + 16f);

            // Dark pill background
            Image bg = quickbarRoot.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.10f, 0.82f);

            quickSlotObjs.Clear();

            float startX = -(totalW / 2f - slotSize / 2f - 8f);

            for (int i = 0; i < visibleSlots; i++)
            {
                GameObject slot = BuildSlot(quickbarRoot.transform, $"QSlot_{i}",
                    new Vector2(startX + i * (slotSize + spacing), 0f), slotSize, i + 1);
                quickSlotObjs.Add(slot);

                // Click handler
                int capturedIndex = i;
                Button btn = slot.AddComponent<Button>();
                btn.onClick.AddListener(() => { UseItem(capturedIndex); FlashQuickSlot(capturedIndex); });
                btn.targetGraphic = slot.GetComponent<Image>();
                ColorBlock cb = btn.colors;
                cb.highlightedColor = new Color(0.2f, 0.6f, 1f, 0.3f);
                cb.pressedColor     = new Color(0.1f, 0.4f, 0.9f, 0.5f);
                btn.colors = cb;
            }
        }

        // ─────────────────────── Full Inventory Panel ───────────────────────

        private void BuildInventoryPanel()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            Transform prev = canvas.transform.Find("InventoryPanel");
            if (prev != null) Destroy(prev.gameObject);

            inventoryPanel = new GameObject("InventoryPanel", typeof(RectTransform));
            inventoryPanel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = inventoryPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot     = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(540f, 420f);

            // Dark semi-transparent backdrop
            Image panelBg = inventoryPanel.AddComponent<Image>();
            panelBg.color = new Color(0.04f, 0.07f, 0.13f, 0.96f);

            // Neon border outline
            Outline outline = inventoryPanel.AddComponent<Outline>();
            outline.effectColor    = new Color(0f, 0.85f, 1f, 0.7f);
            outline.effectDistance = new Vector2(2f, 2f);

            // Title bar
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(inventoryPanel.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot     = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -4f);
            titleRect.sizeDelta = new Vector2(0f, 42f);

            Text titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font      = uiFont;
            titleTxt.fontSize  = 20;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color     = new Color(0f, 0.95f, 1f, 1f);
            titleTxt.text      = "INVENTORY  [TAB]";

            // Hint text
            GameObject hintObj = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            hintObj.transform.SetParent(inventoryPanel.transform, false);
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot     = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 10f);
            hintRect.sizeDelta = new Vector2(0f, 24f);

            Text hintTxt = hintObj.GetComponent<Text>();
            hintTxt.font      = uiFont;
            hintTxt.fontSize  = 11;
            hintTxt.alignment = TextAnchor.MiddleCenter;
            hintTxt.color     = new Color(0.6f, 0.65f, 0.7f, 0.9f);
            hintTxt.text      = "Click item to use  |  Keys 1-8 for hotkeys  |  TAB / ESC to close";

            // Grid container
            GameObject grid = new GameObject("Grid", typeof(RectTransform));
            grid.transform.SetParent(inventoryPanel.transform, false);
            RectTransform gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.05f, 0.12f);
            gridRect.anchorMax = new Vector2(0.95f, 0.88f);
            gridRect.sizeDelta = Vector2.zero;

            GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
            glg.cellSize        = new Vector2(100f, 100f);
            glg.spacing         = new Vector2(10f, 10f);
            glg.padding         = new RectOffset(10, 10, 10, 10);
            glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment  = TextAnchor.UpperLeft;
            glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;

            fullSlotObjs.Clear();

            for (int i = 0; i < maxSlots; i++)
            {
                GameObject slot = BuildFullSlot(grid.transform, i);
                fullSlotObjs.Add(slot);
            }

            inventoryPanel.SetActive(false);
        }

        // ─────────────────────── Slot Builders ───────────────────────

        private GameObject BuildSlot(Transform parent, string name, Vector2 pos, float size, int hotkey)
        {
            GameObject slot = new GameObject(name, typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(parent, false);

            RectTransform r = slot.GetComponent<RectTransform>();
            r.sizeDelta        = new Vector2(size, size);
            r.anchoredPosition = pos;

            Image bg = slot.GetComponent<Image>();
            bg.color = new Color(0.09f, 0.12f, 0.20f, 0.95f);

            // Cyan border
            AddBorderImage(slot.transform, new Color(0f, 0.82f, 1f, 0.35f));

            // Hotkey number (top-left)
            AddLabel(slot.transform, hotkey.ToString(), 10,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(5f, -3f), new Vector2(20f, 16f),
                new Color(0.3f, 0.9f, 1f, 0.9f), FontStyle.Bold, TextAnchor.UpperLeft);

            // Symbol / emoji (center-top)
            AddLabel(slot.transform, "?", 22,
                new Vector2(0f, 0.35f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                new Color(0.9f, 0.9f, 0.9f, 1f), FontStyle.Normal, TextAnchor.MiddleCenter,
                "SymbolText");

            // Item name (bottom)
            AddLabel(slot.transform, "", 9,
                new Vector2(0f, 0f), new Vector2(1f, 0.38f), new Vector2(0.5f, 0f),
                new Vector2(2f, 0f), new Vector2(-2f, 0f),
                new Color(0.85f, 0.90f, 1f, 1f), FontStyle.Bold, TextAnchor.MiddleCenter,
                "NameText");

            // Count badge (bottom-right)
            AddLabel(slot.transform, "", 10,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-3f, 3f), new Vector2(28f, 16f),
                new Color(1f, 0.80f, 0.2f, 1f), FontStyle.Bold, TextAnchor.LowerRight,
                "CountText");

            return slot;
        }

        private GameObject BuildFullSlot(Transform parent, int index)
        {
            GameObject slot = new GameObject($"FullSlot_{index}", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(parent, false);

            Image bg = slot.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.11f, 0.19f, 0.95f);

            AddBorderImage(slot.transform, new Color(0f, 0.75f, 1f, 0.3f));

            // Hotkey badge
            AddLabel(slot.transform, (index + 1).ToString(), 11,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(5f, -4f), new Vector2(22f, 18f),
                new Color(0.3f, 0.9f, 1f, 0.85f), FontStyle.Bold, TextAnchor.UpperLeft);

            // Symbol (large, center)
            AddLabel(slot.transform, "", 28,
                new Vector2(0f, 0.4f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 1f, 1f, 1f), FontStyle.Normal, TextAnchor.MiddleCenter,
                "SymbolText");

            // Name
            AddLabel(slot.transform, "", 10,
                new Vector2(0f, 0f), new Vector2(1f, 0.42f), new Vector2(0.5f, 0f),
                new Vector2(3f, 0f), new Vector2(-3f, 0f),
                new Color(0.85f, 0.90f, 1f, 1f), FontStyle.Bold, TextAnchor.MiddleCenter,
                "NameText");

            // Count badge
            AddLabel(slot.transform, "", 11,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-4f, 4f), new Vector2(32f, 18f),
                new Color(1f, 0.80f, 0.2f, 1f), FontStyle.Bold, TextAnchor.LowerRight,
                "CountText");

            // Click to use
            Button btn = slot.AddComponent<Button>();
            btn.onClick.AddListener(() => { UseItem(index); });
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor      = new Color(1f, 1f, 1f, 1f);
            cb.highlightedColor = new Color(0.2f, 0.65f, 1f, 0.35f);
            cb.pressedColor     = new Color(0.1f, 0.4f, 0.9f, 0.5f);
            btn.colors = cb;

            return slot;
        }

        // ─────────────────────── UI Refresh ───────────────────────

        private void RefreshAll()
        {
            RefreshSlotList(quickSlotObjs);
            RefreshSlotList(fullSlotObjs);
        }

        private void RefreshSlotList(List<GameObject> slotObjs)
        {
            for (int i = 0; i < slotObjs.Count; i++)
            {
                if (slotObjs[i] == null) continue;

                Text symbol = slotObjs[i].transform.Find("SymbolText")?.GetComponent<Text>();
                Text name   = slotObjs[i].transform.Find("NameText")?.GetComponent<Text>();
                Text count  = slotObjs[i].transform.Find("CountText")?.GetComponent<Text>();
                Image bg    = slotObjs[i].GetComponent<Image>();
                Image border= slotObjs[i].transform.Find("Border")?.GetComponent<Image>();

                if (i < items.Count)
                {
                    InventoryItem item = items[i];
                    string sym = GetSymbol(item.itemName);

                    if (symbol != null) symbol.text = sym;
                    if (name   != null) name.text   = ShortenName(item.itemName);
                    if (count  != null) count.text  = item.count > 1 ? $"x{item.count}" : "";
                    if (bg     != null) bg.color    = new Color(0.10f, 0.14f, 0.23f, 0.97f);
                    if (border != null) border.color = GetCategoryColor(item.category ?? GetCategory(item.itemName));
                }
                else
                {
                    if (symbol != null) symbol.text = "";
                    if (name   != null) name.text   = "";
                    if (count  != null) count.text  = "";
                    if (bg     != null) bg.color    = new Color(0.06f, 0.08f, 0.13f, 0.60f);
                    if (border != null) border.color = new Color(0.2f, 0.25f, 0.35f, 0.20f);
                }
            }
        }

        private void FlashQuickSlot(int index)
        {
            if (index >= quickSlotObjs.Count) return;
            Image border = quickSlotObjs[index].transform.Find("Border")?.GetComponent<Image>();
            if (border != null) StartCoroutine(FlashBorder(border));
        }

        private System.Collections.IEnumerator FlashBorder(Image border)
        {
            Color orig = border.color;
            border.color = Color.white;
            yield return new WaitForSeconds(0.15f);
            border.color = orig;
        }

        // ─────────────────────── Helpers ───────────────────────

        private string GetSymbol(string itemName)
        {
            return ItemSymbols.TryGetValue(itemName, out string s) ? s : "\u25A1"; // □ fallback
        }

        private string ShortenName(string n)
        {
            if (n.Length <= 7) return n;
            // Abbreviate Water Bottle → Water
            if (n == "Water Bottle") return "Water";
            return n.Substring(0, 6) + ".";
        }

        private string GetCategory(string itemName)
        {
            if (itemName == "Sword" || itemName == "Bow" || itemName == "Arrow") return "weapon";
            if (itemName == "Berries" || itemName == "Water Bottle" || itemName == "Bandage") return "consumable";
            return "material";
        }

        private Color GetCategoryColor(string category)
        {
            switch (category)
            {
                case "weapon":     return new Color(1.0f, 0.35f, 0.25f, 0.70f); // red-orange
                case "consumable": return new Color(0.25f, 1.0f, 0.45f, 0.60f); // green
                default:           return new Color(0.0f,  0.85f, 1.0f,  0.50f); // cyan (material)
            }
        }

        // Build a full-stretch border image under the slot
        private void AddBorderImage(Transform parent, Color color)
        {
            GameObject b = new GameObject("Border", typeof(RectTransform), typeof(Image));
            b.transform.SetParent(parent, false);
            RectTransform r = b.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.sizeDelta = Vector2.zero;
            Image img = b.GetComponent<Image>();
            img.color = color;

            // Outline hack: use the same outline component
            Outline o = b.AddComponent<Outline>();
            o.effectColor    = color;
            o.effectDistance = new Vector2(1f, 1f);
        }

        // Generic label helper
        private Text AddLabel(Transform parent, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 offsetMin, Vector2 offsetMax,
            Color color, FontStyle style, TextAnchor alignment,
            string objName = "Label")
        {
            GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);

            RectTransform r = obj.GetComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.pivot     = pivot;
            r.offsetMin = offsetMin;
            r.offsetMax = offsetMax;

            Text t = obj.GetComponent<Text>();
            t.font      = uiFont;
            t.fontSize  = fontSize;
            t.fontStyle = style;
            t.alignment = alignment;
            t.color     = color;
            t.text      = text;
            return t;
        }
    }
}
