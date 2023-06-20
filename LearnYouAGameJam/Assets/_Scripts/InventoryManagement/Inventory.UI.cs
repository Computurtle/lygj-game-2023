using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LYGJ.Common;
using LYGJ.Common.Datatypes.Collections;
using LYGJ.EntitySystem.PlayerManagement;
using LYGJ.InventoryManagement;
using OneOf;
using OneOf.Types;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LYGJ {
    public sealed class Inventory_UI : MonoBehaviour {

        void Awake() {
            _PageLeft.onClick.AddListener(OnPageLeft);
            _PageRight.onClick.AddListener(OnPageRight);
            _GroupLeft.onClick.AddListener(OnGroupLeft);
            _GroupRight.onClick.AddListener(OnGroupRight);

            _CanvasGroup.alpha          = 0f;
            _CanvasGroup.interactable   = false;
            _CanvasGroup.blocksRaycasts = false;

            _ItemPreviewCanvasGroup.alpha          = 0f;
            _ItemPreviewCanvasGroup.interactable   = false;
            _ItemPreviewCanvasGroup.blocksRaycasts = false;
        }

        void Start() {
            Inventory.Changed += UpdateUI;
            void UpdateUI( Inventory.ChangeType Type, ItemInstance Item ) => Repaint();
        }

        [SerializeField, Tooltip("The parent for inventory slots."), Required, ChildGameObjectsOnly]
        RectTransform _SlotParent = null!;
        [SerializeField, Tooltip("The prefab for inventory slots."), Required, AssetsOnly]
        Inventory_Slot _SlotPrefab = null!;

        IReadOnlyList<ItemInstance> GetItems() {
            if (_CurrentGroup.TryPickT0(out ItemGroup Group)) {
                return Inventory.OfGroup(Group).ToArray();
            }

            return Inventory.All.ToList();
        }

        #region Slots

        int UsedSlots => GetItems().Count;
        void SpawnSlots() {
            Pool<Inventory_Slot>.ReturnAll(_SlotParent);
            IEnumerable<ItemInstance> Items = GetItems();
            if (_Paginate) {
                Items = Items.Skip(_CurrentPage * _SlotsPerPage).Take(_SlotsPerPage);
            }

            if (_PrefillEmptySlots) {
                List<ItemInstance> Iterated = Items.ToList();

                int EmptySlots = _SlotsPerPage - Iterated.Count;
                if (EmptySlots > 0) {
                    Iterated.AddRange(Enumerable.Repeat(ItemInstance.Empty, EmptySlots));
                }
                Items = Iterated;
            }

            foreach (ItemInstance Item in Items) {
                Inventory_Slot Slot = Pool<Inventory_Slot>.Get(_SlotParent, _SlotPrefab);
                Slot.SetItem(this, Item);
            }
        }

        void Repaint_Slots() => SpawnSlots();

        #endregion

        #region Paging

        [Title("Paging")]
        [SerializeField, Tooltip("Whether to use pagination."), ToggleLeft]
        bool _Paginate = true;
        [SerializeField, Tooltip("The max number of slots per page."), Min(1), ShowIf(nameof(_Paginate))]
        int _SlotsPerPage = 20;
        [SerializeField, Tooltip("Whether to prefill available slots with empty slots."), ShowIf(nameof(_Paginate))]
        bool _PrefillEmptySlots = true;
        [SerializeField, Tooltip("The page left button."), Required, ChildGameObjectsOnly, ShowIf(nameof(_Paginate))]
        Button _PageLeft = null!;
        [SerializeField, Tooltip("The page right button."), Required, ChildGameObjectsOnly, ShowIf(nameof(_Paginate))]
        Button _PageRight = null!;
        [SerializeField, Tooltip("The page number text component."), Required, ChildGameObjectsOnly, ShowIf(nameof(_Paginate))]
        TMP_Text _PageNumber = null!;
        [SerializeField, Tooltip("The page number format string.\n\n{0} = Current Page\n{1} = Total Pages"), ShowIf(nameof(_Paginate))]
        string _PageNumberFormat = "Page {0}/{1}";

        [ShowInInspector, ReadOnly, HideInEditorMode, Tooltip("The current page."), ShowIf(nameof(_Paginate))]
        int _CurrentPage = 0;

        int TotalPages => Mathf.CeilToInt(UsedSlots / (float)_SlotsPerPage);

        public void OnPageLeft() {
            _CurrentPage = Mathf.Max(0, _CurrentPage - 1);
            Repaint();
        }

        public void OnPageRight() {
            _CurrentPage = Mathf.Min(TotalPages - 1, _CurrentPage + 1);
            Repaint();
        }

        void Repaint_Paging() {
            int Total = TotalPages;
            _CurrentPage = Mathf.Min(Total - 1, _CurrentPage + 1); // Re-clamp to ensure we're not on an invalid page (i.e. if an item was just removed)

            _PageNumber.text = string.Format(_PageNumberFormat, _CurrentPage + 1, Total == 0 ? 1 : Total);
        }

        #endregion

        #region Groups

        [Title("Grouping")]
        [SerializeField, Tooltip("Whether to always show all groups, or only those with items."), ToggleLeft]
        bool _AlwaysShowAllGroups = true;
        [SerializeField, Tooltip("The parent for inventory groups."), Required, ChildGameObjectsOnly]
        RectTransform _GroupParent = null!;
        [SerializeField, Tooltip("The prefab for inventory groups."), Required, AssetsOnly]
        Inventory_Group _GroupPrefab = null!;
        [SerializeField, Tooltip("The group left button."), Required, ChildGameObjectsOnly]
        Button _GroupLeft = null!;
        [SerializeField, Tooltip("The group right button."), Required, ChildGameObjectsOnly]
        Button _GroupRight = null!;
        [SerializeField, Tooltip("Whether to include the all group when navigating groups via the left/right buttons.")]
        bool _NavigationIncludesAllGroup = true;

        [Space]
        [SerializeField, Tooltip("The icon for the all group."), Required, AssetsOnly]
        Sprite _GroupAllIcon = null!;
        [SerializeField, Tooltip("The icons for each item group."), Required, AssetsOnly]
        EnumDictionary<ItemGroup, Sprite> _GroupIcons = new(null!);

        [Space]
        [SerializeField, Tooltip("Whether to limit the amount of shown groups at a given time."), ToggleLeft]
        bool _LimitGroups = true;
        [SerializeField, Tooltip("The max number of groups to show at a given time (excluding 'All')."), Min(1), ShowIf(nameof(_LimitGroups))]
        int _ShownGroups = 5;
        // Will be centred around the current group (i.e. if max is 5, and current is 3, then 1, 2, [3], 4, 5 will be shown)
        // If there are less than the max, then all will be shown
        // Also, say there is a max of 5, the current is 6, and there are 7 groups, then 3, 4, 5, [6], 7 will be shown (not centred)

        /// <summary> Gets the icon for the given group. </summary>
        /// <param name="Group"> The group to get the icon for, or <see langword="null"/> for the all group. </param>
        /// <returns> The icon for the given group. </returns>
        public Sprite GetGroupIcon( ItemGroupOrNone Group ) => Group.TryPickT0(out ItemGroup GroupT0) ? _GroupIcons[GroupT0] : _GroupAllIcon;

        [ShowInInspector, ReadOnly, HideInEditorMode, Tooltip("The current group, or null if no group filter is selected.")]
        ItemGroupOrNone _CurrentGroup = ItemGroupOrNone.None;

        public void OnGroupLeft() {
            _CurrentGroup = _CurrentGroup.GetPrevious(_NavigationIncludesAllGroup);
            Repaint();
        }

        public void OnGroupRight() {
            _CurrentGroup = _CurrentGroup.GetNext(_NavigationIncludesAllGroup);
            Repaint();
        }

        public void OnGroupAll() {
            _CurrentGroup = ItemGroupOrNone.None;
            Repaint();
        }

        public void OnGroup( ItemGroup Group ) {
            _CurrentGroup = Group;
            Repaint();
        }

        public void OnGroup( ItemGroupOrNone GroupOrNone ) {
            if (GroupOrNone.TryPickT0(out ItemGroup Group)) {
                OnGroup(Group);
            } else {
                OnGroupAll();
            }
        }

        IEnumerable<ItemGroup> GetGroups() {
            if (_AlwaysShowAllGroups) {
                return Enum<ItemGroup>.Values;
            }

            return Enum<ItemGroup>.Values.Where(Group => Inventory.OfGroup(Group).Any());
        }

        IEnumerable<ItemGroup> GetVisibleGroups() {
            if (_LimitGroups) {
                IReadOnlyCollection<ItemGroup> Groups = GetGroups().AsReadOnlyCollection();

                // Old Algorithm:
                // int Max   = Mathf.Min(_ShownGroups, Groups.Count);
                // int Start = Mathf.Max(0, _CurrentGroup.GetIndex(_NavigationIncludesAllGroup) - Max / 2);
                // return Groups.Skip(Start).Take(Max);

                // This is close to what we want, but it doesn't handle the case where the current group is near the end of the list.
                // The new algorithm first checks the remaining space at the end of the sequence, and if there is not enough groups to show after our current, it will show more at the start.
                int Max   = Mathf.Min(_ShownGroups, Groups.Count);
                int Start = Mathf.Max(0, _CurrentGroup.GetIndex(_NavigationIncludesAllGroup) - Max / 2);
                int End   = Mathf.Min(Groups.Count, Start + Max);
                int Left  = Max - (End - Start);

                if (Left > 0) {
                    Start = Mathf.Max(0, Start - Left);
                }

                return Groups.Skip(Start).Take(Max);
            }

            return GetGroups();
        }

        void SpawnGroupButtons() {
            Pool<Inventory_Group>.ReturnAll(_GroupParent);

            Inventory_Group All = Pool<Inventory_Group>.Get(_GroupParent, _GroupPrefab);
            All.SetAll(this);

            foreach (ItemGroup Group in GetVisibleGroups()) {
                Inventory_Group NewGroup = Pool<Inventory_Group>.Get(_GroupParent, _GroupPrefab);
                NewGroup.SetGroup(this, Group);
            }

            ReorderGroupButtons();
        }

        void ReorderGroupButtons() {
            List<Inventory_Group> Groups = Pool<Inventory_Group>.Active(_GroupParent).ToList();
            Groups.Sort(
                ( A, B ) => {
                    if (!A.Group.TryPickT0(out ItemGroup AGroup)) {
                        return -1;
                    }

                    if (!B.Group.TryPickT0(out ItemGroup BGroup)) {
                        return 1;
                    }

                    return AGroup.CompareTo(BGroup);
                }
            );

            for (int I = 0; I < Groups.Count; I++) {
                Groups[I].transform.SetSiblingIndex(I);
            }

            int  Ln        = _GroupParent.childCount;
            bool FoundLeft = false, FoundRight = false;
            foreach (Button B in _GroupParent.GetComponentsInChildren<Button>()) {
                if (B == _GroupLeft) {
                    B.transform.SetSiblingIndex(0);
                    FoundLeft = true;
                    if (FoundRight) { break; }
                } else if (B == _GroupRight) {
                    B.transform.SetSiblingIndex(Ln - 1);
                    FoundRight = true;
                    if (FoundLeft) { break; }
                }
            }
        }

        void Repaint_Groups() {
            SpawnGroupButtons();
            foreach (Inventory_Group Group in _GroupParent.GetComponentsInChildren<Inventory_Group>()) {
                bool Selected = Group.Group == _CurrentGroup;
                Group.SetSelected(Selected);
            }
        }

        #endregion

        #region Item Preview

        [Title("Item Preview")]
        [SerializeField, Tooltip("The canvas group for the item preview UI."), LabelText("Canvas Group"), Required, ChildGameObjectsOnly]
        CanvasGroup _ItemPreviewCanvasGroup = null!;
        [SerializeField, Tooltip("The time, in seconds, to fade in the item preview UI."), LabelText("Fade In Time"), Min(0), SuffixLabel("s")]
        float _ItemPreviewFadeInTime = 0.25f;
        [SerializeField, Tooltip("The time, in seconds, to fade out the item preview UI."), LabelText("Fade Out Time"), Min(0), SuffixLabel("s")]
        float _ItemPreviewFadeOutTime = 0.25f;
        [SerializeField, Tooltip("The item preview UI."), Required, ChildGameObjectsOnly]
        Inventory_ItemPreview _ItemPreview = null!;

        IEnumerator? _ItemPreviewFade;

        void FadeInItemPreview()  => FadeTo(this, ref _ItemPreviewFade, _ItemPreviewCanvasGroup, 1, _ItemPreviewFadeInTime, true);
        void FadeOutItemPreview() => FadeTo(this, ref _ItemPreviewFade, _ItemPreviewCanvasGroup, 0, _ItemPreviewFadeOutTime, false);

        /// <summary> Shows the given item in the item preview UI. </summary>
        /// <param name="Item"> The item to show. </param>
        public void ShowPreview( ItemInstance Item ) {
            _ItemPreview.SetItem(this, Item);
            FadeInItemPreview();
        }

        /// <summary> Hides the item preview UI. </summary>
        public void HidePreview() {
            FadeOutItemPreview();
        }

        void Repaint_Preview() {
            // If the preview is still open:
            // - Check if the item still exists, if it does, update the preview with the new amount.
            // - If the item doesn't exist anymore, close the preview.
            if (_ItemPreviewCanvasGroup.alpha > 0) {
                ItemInstance Item  = _ItemPreview.Item;
                uint         Count = Inventory.Count(Item);
                if (Count > 0u) {
                    _ItemPreview.SetItem(this, new(Item,Count));
                } else {
                    HidePreview();
                }
            }
        }

        #endregion

        #region Visibility

        [Title("Visibility")]
        [SerializeField, Tooltip("The canvas group for the inventory UI."), Required, ChildGameObjectsOnly]
        CanvasGroup _CanvasGroup = null!;
        [SerializeField, Tooltip("The time, in seconds, to fade in the inventory UI."), Min(0), SuffixLabel("s")]
        float _FadeInTime = 0.25f;
        [SerializeField, Tooltip("The time, in seconds, to fade out the inventory UI."), Min(0), SuffixLabel("s")]
        float _FadeOutTime = 0.25f;

        [ShowInInspector, ReadOnly, HideInEditorMode, Tooltip("The current visibility of the inventory UI.")]
        bool _Visible = false;

        static IEnumerator FadeTo( CanvasGroup Group, float End, float Duration, bool Usable ) {
            float Start   = Group.alpha;
            float Elapsed = 0;
            Group.interactable   = false;
            Group.blocksRaycasts = false;
            while (Elapsed < Duration) {
                Elapsed            += Time.deltaTime;
                Group.alpha =  Mathf.Lerp(Start, End, Elapsed / Duration);
                yield return null;
            }

            Group.alpha = End;
            if (Usable) {
                Group.interactable   = true;
                Group.blocksRaycasts = true;
            }
        }
        static void FadeTo(MonoBehaviour Target, ref IEnumerator? Fade, CanvasGroup Group, float End, float Duration, bool Usable) {
            if (Fade is not null) {
                Target.StopCoroutine(Fade);
            }

            Fade = FadeTo(Group, End, Duration, Usable);
            Target.StartCoroutine(Fade);
        }
        IEnumerator? _Fade = null;
        void FadeIn() => FadeTo(this, ref _Fade, _CanvasGroup, 1, _FadeInTime, true);
        void FadeOut() => FadeTo(this, ref _Fade, _CanvasGroup, 0, _FadeOutTime, false);

        /// <summary> Gets or sets the visibility of the inventory UI. </summary>
        public bool Visible {
            get => _Visible;
            set {
                if (_Visible == value) { return; }

                _Visible = value;
                if (_Visible) {
                    if (_RepaintQueued) {
                        Repaint();
                    }

                    Pointer.SetVisible(PointerPriority.Inventory);
                    FadeIn();
                } else {
                    Pointer.ClearVisible(PointerPriority.Inventory);
                    FadeOut();
                }
            }
        }

        bool _RepaintQueued = true; // When the inventory is not visible, we queue a single repaint for when it becomes visible again. Default to true so the first time the inventory is shown, it (re)paints.

        #endregion

        #if UNITY_EDITOR
        [Button("Show"), ButtonGroup("ShowHide"), DisableIf(nameof(Visible)), HideInEditorMode]
        void Editor_Show() => Visible = true;
        [Button("Hide"), ButtonGroup("ShowHide"), EnableIf(nameof(Visible)), HideInEditorMode]
        void Editor_Hide() => Visible = false;
        #endif

        [Button, HideInEditorMode]
        void Repaint() {
            if (!Visible) {
                _RepaintQueued = true;
                return;
            } // Don't repaint if the inventory is hidden.

            _RepaintQueued = false;

            Repaint_Paging();
            Repaint_Groups();
            Repaint_Slots();
            Repaint_Preview();
        }
    }

    public sealed class ItemGroupOrNone : OneOfBase<ItemGroup, None>, IEquatable<ItemGroupOrNone> {

        /// <inheritdoc />
        ItemGroupOrNone( OneOf<ItemGroup, None> Input ) : base(Input) { }

        /// <summary> Gets the next group in the sequence. </summary>
        /// <param name="IncludeNone"> Whether to include the 'None' value in the sequence. </param>
        /// <returns> The next group in the sequence. </returns>
        public ItemGroupOrNone GetNext( bool IncludeNone ) {
            if (TryPickT0(out ItemGroup Group)) {
                if (IncludeNone) {
                    ItemGroup Next = Group.Next(Loop: false);
                    if (Next == Group) { // If we fail to get the next, that means we're at the end of the sequence.
                        return None;
                    }
                }

                return Group.Next();
            }

            return Enum<ItemGroup>.First;
        }

        /// <summary> Gets the previous group in the sequence. </summary>
        /// <param name="IncludeNone"> Whether to include the 'None' value in the sequence. </param>
        /// <returns> The previous group in the sequence. </returns>
        public ItemGroupOrNone GetPrevious( bool IncludeNone ) {
            if (TryPickT0(out ItemGroup Group)) {
                if (IncludeNone) {
                    ItemGroup Previous = Group.Previous(Loop: false);
                    if (Previous == Group) { // If we fail to get the previous, that means we're at the start of the sequence.
                        return None;
                    }
                }

                return Group.Previous();
            }

            return Enum<ItemGroup>.Last;
        }

        /// <summary> Gets the index of the group in the sequence. </summary>
        /// <param name="IncludeNone"> Whether to include the 'None' value in the sequence. </param>
        /// <returns> The index of the group in the sequence. </returns>
        public int GetIndex( bool IncludeNone ) {
            if (TryPickT0(out ItemGroup Group)) {
                if (IncludeNone) {
                    ItemGroup Previous = Group.Previous(Loop: false);
                    if (Previous == Group) { // If we fail to get the previous, that means we're at the start of the sequence.
                        return 0;
                    }
                }

                return Group.IndexOf();
            }

            return Enum<ItemGroup>.Count - 1;
        }

        public static implicit operator ItemGroupOrNone( ItemGroup Group ) => new(Group);
        public static implicit operator ItemGroupOrNone( None      None )  => new(None);

        /// <summary> Gets a 'None' value. </summary>
        public static ItemGroupOrNone None => new None();

        /// <inheritdoc cref="OneOfBase{T0,T1}.TryPickT0" />
        public bool TryPickT0( out ItemGroup Group ) => TryPickT0(out Group, out _);

        #region Equality Members

        /// <inheritdoc />
        public bool Equals( ItemGroupOrNone Other ) => base.Equals(Other);

        /// <inheritdoc />
        public override bool Equals( object? Obj ) => ReferenceEquals(this, Obj) || Obj is ItemGroupOrNone Other && Equals(Other);

        /// <inheritdoc />
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==( ItemGroupOrNone? Left, ItemGroupOrNone? Right ) => Equals(Left, Right);
        public static bool operator !=( ItemGroupOrNone? Left, ItemGroupOrNone? Right ) => !Equals(Left, Right);

        #endregion

    }

}
