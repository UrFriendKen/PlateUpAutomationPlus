using Kitchen;
using Kitchen.Modules;
using KitchenLib;
using KitchenLib.Event;
using KitchenLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KitchenAutomationPlus.Preferences
{
    public class PreferencesManager
    {
        public enum MenuType
        {
            MainMenu,
            PauseMenu
        }

        public enum MenuAction
        {
            MainMenuNull,
            MainMenuStartSingleplayer,
            MainMenuQuit,
            MainMenuStartMultiplayer,
            MainMenuBack,

            PauseMenuCloseMenu,
            PauseMenuBack,
            PauseMenuDisconnectPlayer,
            PauseMenuQuit,
            PauseMenuAbandonRestaurant,
            PauseMenuOpenInvitePanel,
            PauseMenuLeaveGame,
            PauseMenuPracticeMode
        }

        public readonly string MOD_GUID;
        public readonly string MOD_NAME;

        private bool _isKLPreferencesEventsRegistered = false;
        public bool IsKLPreferencesEventsRegistered
        {
            get { return _isKLPreferencesEventsRegistered; }
        }

        AssemblyBuilder _assemblyBuilder;
        ModuleBuilder _moduleBuilder;

        private static readonly Regex sWhitespace = new Regex(@"\s+");

        private static List<string> keys = new List<string>();
        private static Dictionary<string, KitchenLib.BoolPreference> boolPreferences = new Dictionary<string, KitchenLib.BoolPreference>();
        private static Dictionary<string, KitchenLib.IntPreference> intPreferences = new Dictionary<string, KitchenLib.IntPreference>();
        private static Dictionary<string, KitchenLib.FloatPreference> floatPreferences = new Dictionary<string, KitchenLib.FloatPreference>();
        private static Dictionary<string, KitchenLib.StringPreference> stringPreferences = new Dictionary<string, StringPreference>();


        private static readonly Type[] allowedTypes = new Type[]
        {
            typeof(bool),
            typeof(int),
            typeof(float),
            typeof(string)
        };


        private bool _mainMenuRegistered = false;
        private bool _pauseMenuRegistered = false;
        Type _mainTopLevelTypeKey = null;
        Type _pauseTopLevelTypeKey = null;
        private Queue<Type> _mainMenuTypeKeys = new Queue<Type>();
        private Queue<Type> _pauseMenuTypeKeys = new Queue<Type>();
        private Stack<List<(ElementType, object)>> _elements = new Stack<List<(ElementType, object)>>();
        private Queue<List<(ElementType, object)>> _completedElements = new Queue<List<(ElementType, object)>>();
        internal enum ElementType
        {
            Label,
            Info,
            Select,
            Button,
            PlayerRow,
            SubmenuButton,
            BoolOption,
            IntOption,
            FloatOption,
            StringOption,
            Spacer
        }

        public PreferencesManager(string modGUID, string modName)
        {
            MOD_GUID = modGUID;
            MOD_NAME = modName;

            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{this.GetType().Namespace}.{MOD_GUID}"), AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Module");

            _elements = new Stack<List<(ElementType, object)>>();
            _mainTopLevelTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_Main");
            _pauseTopLevelTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_Pause");
            _elements.Push(new List<(ElementType, object)>());

            _completedElements = new Queue<List<(ElementType, object)>>();
        }

        private static bool IsAllowedType(Type type, bool throwExceptionIfNotAllowed = false)
        {
            if (!allowedTypes.Contains(type))
            {
                if (throwExceptionIfNotAllowed)
                    ThrowTypeException();
                return false;
            }
            return true;
        }

        private static void ThrowTypeException()
        {
            string allowedTypesStr = "";
            for (int i = 0; i < allowedTypes.Length; i++)
            {
                allowedTypesStr += allowedTypes[i].ToString();
                if (i != allowedTypes.Length - 1) allowedTypesStr += ", ";
            }
            throw new ArgumentException($"Type T is not supported! Only use {allowedTypesStr}.");
        }

        private static bool IsUsedKey(string key, bool throwExceptionIfUsed = false)
        {
            if (keys.Contains(key))
            {
                if (throwExceptionIfUsed)
                    ThrowKeyException(key);
                return true;
            }
            return false;
        }

        private static void ThrowKeyException(string key)
        {
            throw new ArgumentException($"Key {key} already exists!");
        }

        private T ChangeType<T>(object obj)
        {
            return (T)Convert.ChangeType(obj, typeof(T));
        }

        private void Preference_OnChanged<T>(string key, T value)
        {
            Set(key, value);
        }

        public void AddOption<T>(string key, string name, T initialValue, T[] values, string[] strings)
        {
            IsAllowedType(typeof(T), true);
            IsUsedKey(key, true);

            if (typeof(T) == typeof(bool))
            {
                KitchenLib.BoolPreference preference = PreferenceUtils.Register<KitchenLib.BoolPreference>(MOD_GUID, key, name);
                boolPreferences.Add(key, preference);
                preference.Value = (bool)Convert.ChangeType( initialValue, typeof(bool));
                EventHandler<bool> handler = delegate (object _, bool b)
                {
                    Preference_OnChanged(key, b);
                };
                _elements.Peek().Add((ElementType.BoolOption, new BoolOptionData(MOD_GUID, key, values.Cast<bool>().ToList(), strings.ToList(), handler)));
            }
            else if (typeof(T) == typeof(int))
            {
                KitchenLib.IntPreference preference = PreferenceUtils.Register<KitchenLib.IntPreference>(MOD_GUID, key, name);
                intPreferences.Add(key, preference);
                preference.Value = (int)Convert.ChangeType(initialValue, typeof(int));
                EventHandler<int> handler = delegate (object _, int i)
                {
                    Preference_OnChanged(key, i);
                };
                _elements.Peek().Add((ElementType.IntOption, new IntOptionData(MOD_GUID, key, values.Cast<int>().ToList(), strings.ToList(), handler)));
            }
            else if (typeof(T) == typeof(float))
            {
                KitchenLib.FloatPreference preference = PreferenceUtils.Register<KitchenLib.FloatPreference>(MOD_GUID, key, name);
                floatPreferences.Add(key, preference);
                preference.Value = (float)Convert.ChangeType(initialValue, typeof(float));
                EventHandler<float> handler = delegate (object _, float f)
                {
                    Preference_OnChanged(key, f);
                };
                _elements.Peek().Add((ElementType.FloatOption, new FloatOptionData(MOD_GUID, key, values.Cast<float>().ToList(), strings.ToList(), handler)));
            }
            else if (typeof(T) == typeof(string))
            {
                KitchenLib.StringPreference preference = PreferenceUtils.Register<KitchenLib.StringPreference>(MOD_GUID, key, name);
                stringPreferences.Add(key, preference);
                preference.Value = (string)Convert.ChangeType(initialValue, typeof(string));
                EventHandler<string> handler = delegate (object _, string s)
                {
                    Preference_OnChanged(key, s);
                };
                _elements.Peek().Add((ElementType.FloatOption, new StringOptionData(MOD_GUID, key, values.Cast<string>().ToList(), strings.ToList(), handler)));
            }
            keys.Add(key);
        }

        public T Get<T>(string key)
        {
            //Load();

            IsAllowedType(typeof(T), true);

            bool found = false;
            object value = default(T);
            if (typeof(T) == typeof(bool))
            {
                KitchenLib.BoolPreference preference = PreferenceUtils.Get<KitchenLib.BoolPreference>(MOD_GUID, key);
                if (preference != null)
                {
                    value = preference.Value;
                    found = true;
                }
            }
            else if (typeof(T) == typeof(int))
            {
                KitchenLib.IntPreference preference = PreferenceUtils.Get<KitchenLib.IntPreference>(MOD_GUID, key);
                if (preference != null)
                {
                    value = preference.Value;
                    found = true;
                }
            }
            else if (typeof(T) == typeof(float))
            {
                KitchenLib.FloatPreference preference = PreferenceUtils.Get<KitchenLib.FloatPreference>(MOD_GUID, key);
                if (preference != null)
                {
                    value = preference.Value;
                    found = true;
                }
            }
            else if (typeof(T) == typeof(string))
            {
                KitchenLib.StringPreference preference = PreferenceUtils.Get<KitchenLib.StringPreference>(MOD_GUID, key);
                if (preference != null)
                {
                    value = preference.Value;
                    found = true;
                }
            }

            if (!found)
            {
                //TODO Throw type mismatch exception. Key exists but type provided is not correct type.
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public void Set<T>(string key, T value)
        {
            IsAllowedType(typeof(T), true);

            bool found = false;
            if (typeof(T) == typeof(bool))
            {
                KitchenLib.BoolPreference preference = PreferenceUtils.Get<KitchenLib.BoolPreference>(MOD_GUID, key);
                if (preference != null)
                {
                    found = true;
                    preference.Value = (bool)Convert.ChangeType(value, typeof(bool));
                }
            }
            else if (typeof(T) == typeof(int))
            {
                KitchenLib.IntPreference preference = PreferenceUtils.Get<KitchenLib.IntPreference>(MOD_GUID, key);
                if (preference != null)
                {
                    found = true;
                    preference.Value = (int)Convert.ChangeType(value, typeof(int));
                }
            }
            else if (typeof(T) == typeof(float))
            {
                KitchenLib.FloatPreference preference = PreferenceUtils.Get<KitchenLib.FloatPreference>(MOD_GUID, key);
                if (preference != null)
                {
                    found = true;
                    preference.Value = (float)Convert.ChangeType(value, typeof(float));
                }
            }
            else if (typeof(T) == typeof(string))
            {
                KitchenLib.StringPreference preference = PreferenceUtils.Get<KitchenLib.StringPreference>(MOD_GUID, key);
                if (preference != null)
                {
                    preference.Value = (string)Convert.ChangeType(value, typeof(string));
                }
            }

            if (found)
            {
                Save();
                return;
            }

            //TODO Throw type mismatch exception. Key exists but value not correct type.
            ThrowTypeException();
        }

        private static void Save()
        {
            PreferenceUtils.Save();
        }

        private static void Load()
        {
            PreferenceUtils.Load();
        }


        public readonly struct LabelData
        {
            public readonly string Text;
            public LabelData(string text)
            {
                Text = text;
            }
        }
        public readonly struct InfoData
        {
            public readonly string Text;
            public InfoData(string text)
            {
                Text = text;
            }
        }
        public readonly struct SelectData
        {
            public readonly List<string> Options;
            public readonly Action<int> OnActivate;
            public readonly int Index;
            public SelectData(List<string> options, Action<int> on_activate, int index = 0)
            {
                Options = options;
                OnActivate = on_activate;
                Index = index;
            }
        }
        public readonly struct ButtonData
        {
            public readonly string ButtonText;
            public readonly Action<int> OnActivate;
            public readonly int Arg;
            public readonly float Scale;
            public readonly float Padding;
            public ButtonData(string button_text, Action<int> on_activate, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                ButtonText = button_text;
                OnActivate = on_activate;
                Arg = arg;
                Scale = scale;
                Padding = padding;
            }
        }
        public readonly struct PlayerRowData
        {
            public readonly string Username;
            public readonly PlayerInfo Player;
            public readonly Action<int> OnKick;
            public readonly Action<int> OnRemove;
            public readonly int Arg;
            public readonly float Scale;
            public readonly float Padding;
            public PlayerRowData(string username, PlayerInfo player, Action<int> on_kick, Action<int> on_remove, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                Username = username;
                Player = player;
                OnKick = on_kick;
                OnRemove = on_remove;
                Arg = arg;
                Scale = scale;
                Padding = padding;
            }
        }
        public readonly struct SubmenuButtonData
        {
            public readonly string ButtonText;
            public readonly Type MainMenuKey;
            public readonly Type PauseMenuKey;
            public readonly bool SkipStack;
            public SubmenuButtonData(string button_text, Type main_menu_key, Type pause_menu_key, bool skip_stack = false)
            {
                ButtonText = button_text;
                MainMenuKey = main_menu_key;
                PauseMenuKey = pause_menu_key;
                SkipStack = skip_stack;
            }
        }
        public readonly struct ActionButtonData
        {
            public readonly string ButtonText;
            public readonly MenuAction Action;
            public readonly ElementStyle Style;
            public ActionButtonData(string button_text, MenuAction action, ElementStyle style = ElementStyle.Default)
            {
                ButtonText = button_text;
                Action = action;
                Style = style;
            }
        }
        public readonly struct BoolOptionData
        {
            public readonly string ModGUID;
            public readonly string Key;
            public readonly List<bool> Values;
            public readonly List<string> Strings;
            public readonly EventHandler<bool> EventHandler;
            public BoolOptionData(string modGuid, string key, List<bool> values, List<string> strings, EventHandler<bool> eventHandler)
            {
                ModGUID = modGuid;
                Key = key;
                Values = values;
                Strings = strings;
                EventHandler = eventHandler;
            }
        }
        public readonly struct IntOptionData
        {
            public readonly string ModGUID;
            public readonly string Key;
            public readonly List<int> Values;
            public readonly List<string> Strings;
            public readonly EventHandler<int> EventHandler;
            public IntOptionData(string modGuid, string key, List<int> values, List<string> strings, EventHandler<int> eventHandler)
            {
                ModGUID = modGuid;
                Key = key;
                Values = values;
                Strings = strings;
                EventHandler = eventHandler;
            }
        }
        public readonly struct FloatOptionData
        {
            public readonly string ModGUID;
            public readonly string Key;
            public readonly List<float> Values;
            public readonly List<string> Strings;
            public readonly EventHandler<float> EventHandler;
            public FloatOptionData(string modGuid, string key, List<float> values, List<string> strings, EventHandler<float> eventHandler)
            {
                ModGUID = modGuid;
                Key = key;
                Values = values;
                Strings = strings;
                EventHandler = eventHandler;
            }
        }
        public readonly struct StringOptionData
        {
            public readonly string ModGUID;
            public readonly string Key;
            public readonly List<string> Values;
            public readonly List<string> Strings;
            public readonly EventHandler<string> EventHandler;
            public StringOptionData(string modGuid, string key, List<string> values, List<string> strings, EventHandler<string> eventHandler)
            {
                ModGUID = modGuid;
                Key = key;
                Values = values;
                Strings = strings;
                EventHandler = eventHandler;
            }
        }

        public void AddLabel(string text)
        {
            _elements.Peek().Add((ElementType.Label, new LabelData(text)));
        }

        public void AddInfo(string text)
        {
            _elements.Peek().Add((ElementType.Info, new InfoData(text)));
        }

        public void AddSelect(List<string> options, Action<int> on_activate, int index = 0)
        {
            _elements.Peek().Add((ElementType.Select, new SelectData(options, on_activate, index)));
        }

        public void AddButton(string button_text, Action<int> on_activate, int arg = 0, float scale = 1f, float padding = 0.2f)
        {
            _elements.Peek().Add((ElementType.Button, new ButtonData(button_text, on_activate, arg, scale, padding)));
        }

        public void AddPlayerRow(string username, PlayerInfo player, Action<int> on_kick, Action<int> on_remove, int arg = 0, float scale = 1f, float padding = 0.2f)
        {
            _elements.Peek().Add((ElementType.PlayerRow, new PlayerRowData(username, player, on_kick, on_remove, arg, scale, padding)));
        }

        public void AddSubmenu(string button_text, string submenu_key, bool skip_stack = false)
        {
            Type mainTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_{sWhitespace.Replace(submenu_key, "")}_Main");
            Type pauseTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_{sWhitespace.Replace(submenu_key, "")}_Pause");
            if (_mainMenuTypeKeys.Contains(mainTypeKey))
            {
                throw new ArgumentException("Submenu key already exists!");
            }
            _mainMenuTypeKeys.Enqueue(mainTypeKey);
            _pauseMenuTypeKeys.Enqueue(pauseTypeKey);
            _elements.Peek().Add((ElementType.SubmenuButton, new SubmenuButtonData(button_text, mainTypeKey, pauseTypeKey, skip_stack)));
            _elements.Push(new List<(ElementType, object)>());
        }

        public void AddActionButton(string button_text, MenuAction action, ElementStyle style = ElementStyle.Default)
        {
            _elements.Peek().Add((ElementType.Button, new ActionButtonData(button_text, action, style)));
        }

        public void AddSpacer()
        {
            _elements.Peek().Add((ElementType.Spacer, null));
        }

        public void SubmenuDone()
        {
            if (_elements.Count < 1)
            {
                throw new Exception("Submenu depth already at highest level.");
            }
            _completedElements.Enqueue(_elements.Pop());
        }

        private Type CreateTypeKey(string typeName)
        {
            //Creating dummy types to use as keys for submenu instances when registering the submenus to keys in KitchenLib CreateSubmenusEvent
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public);
            Type type = typeBuilder.CreateType();
            return type;
        }

        public void RegisterMenu(MenuType menuType)
        {
            Load();

            _mainMenuTypeKeys.Enqueue(_mainTopLevelTypeKey);
            _pauseMenuTypeKeys.Enqueue(_pauseTopLevelTypeKey);
            if (!_isKLPreferencesEventsRegistered)
            {
                _isKLPreferencesEventsRegistered = true;
                while (_elements.Count > 0)
                {
                    _completedElements.Enqueue(_elements.Pop());
                }

                //bool first = true;
                while (_completedElements.Count > 0)
                {
                    List<(ElementType, object)> submenuElements = _completedElements.Dequeue();
                    Type mainMenuKey = _mainMenuTypeKeys.Dequeue();
                    Type pauseMenuKey = _pauseMenuTypeKeys.Dequeue();

                    Events.PreferenceMenu_MainMenu_CreateSubmenusEvent += (s, args) =>
                    {
                        Submenu<MainMenuAction> submenu = new Submenu<MainMenuAction>(args.Container, args.Module_list, submenuElements);
                        args.Menus.Add(mainMenuKey, submenu);
                    };
                    //if (first) _mainTopLevelTypeKey = mainMenuKey;

                    Events.PreferenceMenu_PauseMenu_CreateSubmenusEvent += (s, args) =>
                    {
                        Submenu<PauseMenuAction> submenu = new Submenu<PauseMenuAction>(args.Container, args.Module_list, submenuElements);
                        args.Menus.Add(pauseMenuKey, submenu);
                    };
                    //if (first) _pauseTopLevelTypeKey = pauseMenuKey;

                    //first = false;
                }
            }

            if (menuType == MenuType.MainMenu && !_mainMenuRegistered)
            {
                ModsPreferencesMenu<MainMenuAction>.RegisterMenu(MOD_NAME, _mainTopLevelTypeKey, typeof(MainMenuAction));
                _mainMenuRegistered = true;
            }
            else if (menuType == MenuType.PauseMenu && !_pauseMenuRegistered)
            {
                ModsPreferencesMenu<PauseMenuAction>.RegisterMenu(MOD_NAME, _pauseTopLevelTypeKey, typeof(PauseMenuAction));
                _pauseMenuRegistered = true;
            }
        }

        private class Submenu<T> : KLMenu<T>
        {
            private readonly List<(ElementType, object)> _elements;

            public Submenu(Transform container, ModuleList module_list, List<(ElementType, object)> elements) : base(container, module_list)
            {
                _elements = elements;
            }

            public override void Setup(int player_id)
            {
                foreach (var element in _elements)
                {
                    switch (element.Item1)
                    {
                        case ElementType.Label:
                            AddLabel(((LabelData)element.Item2).Text);
                            break;
                        case ElementType.Info:
                            AddInfo(((InfoData)element.Item2).Text);
                            break;
                        case ElementType.Select:
                            SelectData selectData = (SelectData)element.Item2;
                            AddSelect(selectData.Options, selectData.OnActivate, selectData.Index);
                            break;
                        case ElementType.Button:
                            ButtonData buttonData = (ButtonData)element.Item2;
                            AddButton(buttonData.ButtonText, buttonData.OnActivate, buttonData.Arg, buttonData.Scale, buttonData.Padding);
                            break;
                        case ElementType.PlayerRow:
                            PlayerRowData playerRowData = (PlayerRowData)element.Item2;
                            AddPlayerRow(playerRowData.Username, playerRowData.Player, playerRowData.OnKick, playerRowData.OnRemove, playerRowData.Arg, playerRowData.Scale, playerRowData.Padding);
                            break;
                        case ElementType.SubmenuButton:
                            SubmenuButtonData submenuButtonData = (SubmenuButtonData)element.Item2;
                            Type submenuKey = typeof(T) == typeof(MainMenuAction) ? submenuButtonData.MainMenuKey : submenuButtonData.PauseMenuKey;
                            AddSubmenuButton(submenuButtonData.ButtonText, submenuKey, submenuButtonData.SkipStack);
                            break;
                        case ElementType.BoolOption:
                            BoolOptionData boolOptionData = (BoolOptionData)element.Item2;
                            Option<bool> boolOption = new Option<bool>(boolOptionData.Values, PreferenceUtils.Get<KitchenLib.BoolPreference>(boolOptionData.ModGUID, boolOptionData.Key).Value, boolOptionData.Strings);
                            Add(boolOption);
                            boolOption.OnChanged += boolOptionData.EventHandler;
                            break;
                        case ElementType.IntOption:
                            IntOptionData intOptionData = (IntOptionData)element.Item2;
                            Option<int> intOption = new Option<int>(intOptionData.Values, PreferenceUtils.Get<KitchenLib.IntPreference>(intOptionData.ModGUID, intOptionData.Key).Value, intOptionData.Strings);
                            Add(intOption);
                            intOption.OnChanged += intOptionData.EventHandler;
                            break;
                        case ElementType.FloatOption:
                            FloatOptionData floatOptionData = (FloatOptionData)element.Item2;
                            Option<float> floatOption = new Option<float>(floatOptionData.Values, PreferenceUtils.Get<KitchenLib.FloatPreference>(floatOptionData.ModGUID, floatOptionData.Key).Value, floatOptionData.Strings);
                            Add(floatOption);
                            floatOption.OnChanged += floatOptionData.EventHandler;
                            break;
                        case ElementType.StringOption:
                            StringOptionData stringOptionData = (StringOptionData)element.Item2;
                            Option<string> stringOption = new Option<string>(stringOptionData.Values, PreferenceUtils.Get<KitchenLib.StringPreference>(stringOptionData.ModGUID, stringOptionData.Key).Value, stringOptionData.Strings);
                            Add(stringOption);
                            stringOption.OnChanged += stringOptionData.EventHandler;
                            break;
                        case ElementType.Spacer:
                            New<SpacerElement>();
                            break;
                        default:
                            break;
                    }
                }
                AddButton(base.Localisation["MENU_BACK_SETTINGS"], delegate
                {
                    RequestPreviousMenu();
                });
            }
        }
    }
}
