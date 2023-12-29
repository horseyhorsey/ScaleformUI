using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;
using ScaleformUI.Extensions;

namespace ScaleformUI.Scaleforms
{
    public enum ScoreDisplayType
    {
        NUMBER_ONLY = 0,
        ICON = 1,
        NONE = 2
    };

    public enum ScoreRightIconType
    {
        NONE = 0,
        INACTIVE_HEADSET = 48,
        MUTED_HEADSET = 49,
        ACTIVE_HEADSET = 47,
        RANK_FREEMODE = 65,
        KICK = 64,
        LOBBY_DRIVER = 79,
        LOBBY_CODRIVER = 80,
        SPECTATOR = 66,
        BOUNTY = 115,
        DEAD = 116,
        DPAD_GANG_CEO = 121,
        DPAD_GANG_BIKER = 122,
        DPAD_DOWN_TARGET = 123
    };

    /// <summary> Add player rows, enable or change the CurrentPage to show the scoreboard </summary>
    public class PlayerListHandler
    {
        private int _start;

        private int _timer;

        private int currentPage = 0;

        /// <summary> Initializes player rows </summary>
        public PlayerListHandler() => PlayerRows = new List<PlayerRow>();

        /// <inheritdoc/>
        public delegate void RowChangedEventHandler(PlayerRow player);

        /// <summary> Returns the player when row selection changed </summary>
        public event RowChangedEventHandler SelectionChanged;

        /// <summary> Sets the current page. <para/>
        /// This will use <see cref="NextPage(bool)"/> and set the menu to enabled </summary>
        public int CurrentPage
        {
            get => currentPage;
            set
            {
                SelectedIndex = 0;
                if (PlayerRows.Count == 0)
                {
                    currentPage = 0;
                    return;
                }
                currentPage = value;
                if (currentPage > 0)
                {
                    Enabled = true;
                    NextPage();
                }
            }
        }

        /// <summary>Shows scores for a duration. default 8000</summary>
        public int Duration { get; set; } = 8000;

        /// <summary> Displays rows if enabled </summary>
        public bool Enabled { get; set; }

        /// <summary> Value set by <see cref="UpdateMaxPages(float)"/> depending on player count</summary>
        public int MaxPages { get; set; } = 1;

        /// <summary> These can be reduced but maximum seems to be 17 </summary>
        public int MaxRows { get; set; } = 16;

        /// <summary> Player rows to be displayed in pages</summary>
        public List<PlayerRow> PlayerRows { get; set; }

        /// <summary> Slots visible </summary>
        public int RowsInView { get; private set; }

        /// <summary> Selected index for highlighting</summary>
        public int SelectedIndex { get; private set; }

        /// <summary> Selected player row</summary>
        public int SelectedPlayerRowIndex { get; private set; }

        /// <inheritdoc/>
        public int TitleIcon { get; set; }

        /// <inheritdoc/>
        public string TitleLeftText { get; set; }

        /// <summary> Used for selected/playercount. GTAO displays page/pageCnt</summary>
        public string TitleRightText { get; set; }

        internal ScaleformWideScreen _sc { get; set; }

        private int Index { get; set; } = 0;

        /// <summary> Adds a row to the <see cref="PlayerRows"/> </summary>
        /// <param name="row"></param>
        public void AddRow(PlayerRow row) => PlayerRows.Add(row);

        /// <summary>Creates a Ped mugshot and adds a row to the <see cref="PlayerRows"/> with</summary>
        /// <param name="row">Pedhandle must be assigned on the player row for mugshot</param>
        /// <param name="transparent">transparent</param>
        public async Task AddRow(PlayerRow row, bool transparent)
        {
            if (row.EntityId.HasValue && row.EntityId.Value > 0)
            {
                var mug = await row.EntityId.Value.GetPedMugshotAsync(transparent);
                row.TextureString = mug.Item2;
                UnregisterPedheadshot(mug.Item1);
            }
            AddRow(row);
        }

        /// <summary>Loads the Scaleform using `Load` and builds the menu. Add PlayerRows first then BuildMenu.<para/>
        /// Title is added then all of the <see cref="PlayerRows"/> </summary>
        /// <param name="showTitle">Show title header?</param>
        public async void BuildMenu(bool showTitle = true)
        {
            await Load();
            List<PlayerRow> rows = new();

            if (showTitle)
            {
                _sc.CallFunction("SET_DATA_SLOT_EMPTY");
                _sc.CallFunction("SET_TITLE", TitleLeftText, TitleRightText, TitleIcon);
            }

            Index = 0;
            foreach (PlayerRow row in PlayerRows)
            {
                if (IsRowSupposedToShow(Index))
                {
                    if (row.EntityId.HasValue && row.EntityId.Value > 0)
                    {
                        var mug = await row.EntityId.Value.GetPedMugshotAsync();

                        row.TextureString = mug.Item2;
                        UnregisterPedheadshot(mug.Item1);
                    }

                    rows.Add(row);
                }                    
                Index++;
            }

            RowsInView = rows.Count;

            Index = 0;
            foreach (PlayerRow row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.CrewLabelText))
                    _sc.CallFunction("SET_DATA_SLOT", Index, row.RightText, row.Name, row.Color, (int)row.RightIcon, row.IconOverlayText, row.JobPointsText, "", (int)row.JobPointsDisplayType, row.TextureString, row.TextureString, row.FriendType);
                else
                    _sc.CallFunction("SET_DATA_SLOT", Index, row.RightText, row.Name, row.Color, (int)row.RightIcon, row.IconOverlayText, row.JobPointsText, $"..+{row.CrewLabelText}", (int)row.JobPointsDisplayType, row.TextureString, row.TextureString, row.FriendType);
                Index++;

            }
            
            _sc.CallFunction("DISPLAY_VIEW");
        }        

        /// <summary> Display mic for row </summary>
        /// <param name="idx">row id</param>
        /// <param name="unk"></param>
        public void DisplayMic(int idx, int unk) => _sc.CallFunction("DISPLAY_MIC", idx, unk);

        /// <summary> Cleans up and frees up all ped headshot texture handles </summary>
        public void Dispose()
        {
            if (_sc is null) return;
            Enabled = false;
            Index = 0;
            MaxPages = 1;
            CurrentPage = 0;
            TitleLeftText = "";
            TitleRightText = "";
            TitleIcon = 0;
            _sc.CallFunction("SET_DATA_SLOT_EMPTY");
            _sc.Dispose();
            _sc = null;
            for (int x = 0; x < 1024; x++) // cleaning up in case of a reload, this frees up all ped headshot handles :)
                API.UnregisterPedheadshot(x);
        }

        /// <summary> Moves the next page and runs BuildMenu. The Menu is disposed when the current page exceeds MaxPages</summary>
        /// <param name="updateTitle"></param>
        public void NextPage(bool updateTitle = true)
        {
            UpdateMaxPages(MaxRows);
            _start = Main.GameTime;
            _timer = Duration;
            BuildMenu();
            if (CurrentPage > MaxPages)
            {
                CurrentPage = 0;
                Enabled = false;
                _start = 0;
                Dispose();
                return;
            }

            if (updateTitle)
                UpdateTitleWithPlayers();
        }

        /// <summary> Removes a PlayerRow by existing PlayeRow</summary>
        /// <param name="row"></param>
        public void RemoveRow(PlayerRow row)
        {
            PlayerRow r = PlayerRows.FirstOrDefault(x => x.ServerId == row.ServerId);
            if (r != null)
            {
                PlayerRows.Remove(r);
                if (PlayerRows.Any(x => x.RightText.ToLower() == "lobby")) return;
                PlayerRows.Sort((row1, row2) => Convert.ToInt32(row1.RightText).CompareTo(Convert.ToInt32(row2.RightText)));
            }
        }

        /// <summary> Removes a PlayerRow by player server id</summary>
        /// <param name="serverId"></param>
        public void RemoveRow(int serverId)
        {
            PlayerRow r = PlayerRows.FirstOrDefault(x => x.ServerId == serverId);
            if (r != null)
            {
                PlayerRows.Remove(r);
                PlayerRows.Sort((row1, row2) => Convert.ToInt32(row1.RightText).CompareTo(Convert.ToInt32(row2.RightText)));
            }
        }

        /// <summary> Scaleform SET_HIGHLIGHT </summary>
        /// <param name="idx">row id</param>
        public void SetHighlight(int idx) => _sc?.CallFunction("SET_HIGHLIGHT", idx);

        /// <summary> Selects a player moving up or down</summary>
        /// <param name="down">moving down the list?</param>
        /// <param name="updateTitle">update the header title with players and row index</param>
        public void Select(bool down, bool updateTitle = true)
        {
            if (!down)
            {
                SelectedIndex--;
                if (SelectedIndex < 0)
                    SelectedIndex = RowsInView - 1;
            }
            else
            {
                SelectedIndex++;
                if (SelectedIndex > RowsInView - 1)
                    SelectedIndex = 0;
            }

            //select row by index, hightlight row
            Select(SelectedIndex, updateTitle);
        }

        /// <summary> Selects a row by index</summary>
        /// <param name="idx">visible row index</param>
        /// <param name="updateTitle">update the header title with players and row index</param>
        public void Select(int idx, bool updateTitle = true)
        {
            if (!PlayerRows?.Any() ?? false) return;

            //SelectedIndex = idx;

            //update title with player select/count
            if (updateTitle)
                UpdateTitleWithPlayers();

            //set time back, gives player enough time on the selection
            if(idx > 1)
                _timer = Duration;

            SetHighlight(SelectedIndex);

            if (CurrentPage > 1)
                idx = ((CurrentPage * MaxRows) - MaxRows) + idx;

            SelectionChanged?.Invoke(PlayerRows[idx]);            
        }

        /// <summary>Sets the icon of the given player row index</summary>
        /// <param name="index"></param>
        /// <param name="icon"></param>
        /// <param name="txt"></param>
        public void SetIcon(int index, ScoreRightIconType icon, string txt)
        {
            PlayerRow row = PlayerRows[index];
            if (row != null)
            {
                _sc.CallFunction("SET_ICON", index, (int)icon, txt);
            }
        }

        /// <summary> Sets the Title for the page header </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="icon">Left icon</param>
        public void SetTitle(string left, string right, int icon)
        {
            TitleLeftText = left;
            TitleRightText = right;
            TitleIcon = icon;

            _sc?.CallFunction("SET_TITLE", TitleLeftText, TitleRightText, TitleIcon);
        }

        /// <summary>Gets a mugshot for every player in this page. <para/>
        /// Players must be assigned an EntityId</summary>
        /// <param name="page">Current page</param>
        /// <returns></returns>
        public async Task UpdatePageMugshotsAsync(int page = 0)
        {
            //create skip and take to work on players in list
            page = page > 0 ? page : CurrentPage;
            var skip = page > 1 ? (page - 1) * MaxRows : 0;

            //get players
            var players = PlayerRows
                .Skip(skip)
                .Take(MaxRows);

            foreach (var player in players)
            {
                if (player.EntityId.HasValue && player.EntityId.Value > 0)
                {
                    var mug = await player
                    .EntityId.Value
                    .GetPedMugshotAsync();

                    player.TextureString = mug.Item2;

                    UnregisterPedheadshot(mug.Item1);
                }
            }
        }

        /// <summary>Update slot for an existing player row</summary>
        /// <param name="row"></param>
        public void UpdateSlot(PlayerRow row)
        {
            PlayerRow r = PlayerRows.FirstOrDefault(x => Convert.ToInt32(x.RightText) == Convert.ToInt32(row.RightText));
            //var r = PlayerRows.FirstOrDefault(x => x.ServerId == row.ServerId);
            if (r != null)
            {
                PlayerRows[PlayerRows.IndexOf(r)] = row;
                if (row.CrewLabelText != "")
                    _sc.CallFunction("UPDATE_SLOT", PlayerRows.IndexOf(r), row.RightText, row.Name, row.Color, (int)row.RightIcon, row.IconOverlayText, row.JobPointsText, $"..+{row.CrewLabelText}", (int)row.JobPointsDisplayType, row.TextureString, row.TextureString, row.FriendType);
                else
                    _sc.CallFunction("UPDATE_SLOT", PlayerRows.IndexOf(r), row.RightText, row.Name, row.Color, (int)row.RightIcon, row.IconOverlayText, row.JobPointsText, "", (int)row.JobPointsDisplayType, row.TextureString, row.TextureString, row.FriendType);
            }
        }

        internal void Update()
        {
            API.DrawScaleformMovie(_sc.Handle, 0.122f, 0.3f, 0.28f, 0.6f, 255, 255, 255, 255, 0);
            if (_start != 0 && Main.GameTime - _start > _timer)
            {
                CurrentPage = 0;
                Enabled = false;
                _start = 0;
                Dispose();
                return;
            }
        }

        /// <summary>Used to check if the row from the loop is supposed to be displayed based on the current page view.</summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private bool IsRowSupposedToShow(int row)
        {
            if (CurrentPage > 0)
            {
                int max = CurrentPage * MaxRows;
                int min = CurrentPage * MaxRows - MaxRows;

                if (row >= min && row < max) return true;
            }
            return false;
        }

        /// <summary> Loads the widescreen scaleform `MP_MM_CARD_FREEMODE` </summary>
        /// <returns>wait for scaleform to load with timeout</returns>
        private async Task Load()
        {
            if (_sc is not null) return;
            _sc = new ScaleformWideScreen("MP_MM_CARD_FREEMODE");
            int timeout = 1000;
            int start = Main.GameTime;
            while (!_sc.IsLoaded && Main.GameTime - start < timeout) await BaseScript.Delay(0);
        }

        /// <summary>Updates the max pages to display based on the rows count. <para/>
        /// Rows per page MaxRows</summary>
        private void UpdateMaxPages(float pageCnt = 16f) =>
            MaxPages = (int)Math.Ceiling(PlayerRows.Count / pageCnt);

        /// <summary> Updates the <see cref="TitleRightText"/> with the (_selectedIndex / PlayerCount) and (page/pagecnt)  </summary>
        public void UpdateTitleWithPlayers()
        {
            if (CurrentPage > 1)
            {
                SelectedPlayerRowIndex = SelectedIndex + ((CurrentPage - 1) * MaxRows);
                SetTitle(TitleLeftText, $"({SelectedPlayerRowIndex + 1}/{PlayerRows.Count}) ({CurrentPage}/{MaxPages})", TitleIcon);
            }
            else //only show page count multiple pages
            {
                SelectedPlayerRowIndex = SelectedIndex;
                SetTitle(TitleLeftText, $"({SelectedPlayerRowIndex + 1}/{PlayerRows.Count})", TitleIcon);
            }                            
        }
    }

    /// <summary>
    /// Struct used for the player info row options.
    /// </summary>
    public class PlayerRow
    {
        /// <summary>Color of row</summary>
        public int Color = (int)HudColor.HUD_COLOUR_MID_GREY_MP;

        /// <summary>Crew name</summary>
        public string CrewLabelText = string.Empty;

        /// <summary> ped handle for mugshot</summary>
        public int? EntityId;

        /// <summary>This char is visible over the peds mugshot</summary>
        public char FriendType = '1';
        /// <summary> Job points icon </summary>
        public string IconOverlayText = string.Empty;

        /// <summary> None, Icon, Number</summary>
        public ScoreDisplayType JobPointsDisplayType = ScoreDisplayType.NONE;

        /// <summary> A number of job points </summary>
        public string JobPointsText = string.Empty;

        /// <summary> Player name or name</summary>
        public string Name = string.Empty;

        /// <summary>Right icon, none default</summary>
        public ScoreRightIconType RightIcon = ScoreRightIconType.NONE;

        /// <summary> Right text (Rank Text) </summary>
        public string RightText = string.Empty;

        /// <summary> Player server id</summary>
        public int ServerId;

        /// <summary> Image texture, usually from the players ped mugshot </summary>
        public string TextureString = string.Empty;
    }

    /// <summary> TODO: </summary>
    public class PlayerRowConfig
    {
        public string crewName;
        public int jobPoints;
        public bool showJobPointsIcon;
    }
}
