namespace Lightforge;

public static class Theme
{
    public static readonly Color BgDeep     = Color.FromArgb(21, 21, 21);
    public static readonly Color BgDark     = Color.FromArgb(26, 26, 26);
    public static readonly Color BgPanel    = Color.FromArgb(36, 36, 36);
    public static readonly Color BgSurface  = Color.FromArgb(42, 42, 42);
    public static readonly Color BgInput    = Color.FromArgb(50, 50, 50);
    public static readonly Color BgHover    = Color.FromArgb(55, 55, 58);
    public static readonly Color BgPress    = Color.FromArgb(32, 32, 32);
    public static readonly Color BgSelected = Color.FromArgb(0, 90, 158);
    public static readonly Color Border     = Color.FromArgb(48, 48, 48);
    public static readonly Color BorderLite = Color.FromArgb(60, 60, 60);
    public static readonly Color Accent     = Color.FromArgb(0, 122, 204);
    public static readonly Color AccentHi   = Color.FromArgb(40, 160, 230);
    public static readonly Color Gold       = Color.FromArgb(206, 175, 105);
    public static readonly Color TextBright = Color.FromArgb(230, 230, 230);
    public static readonly Color TextNorm   = Color.FromArgb(190, 190, 190);
    public static readonly Color TextDim    = Color.FromArgb(130, 130, 130);
    public static readonly Color TextMuted  = Color.FromArgb(90, 90, 90);
    public static readonly Color Success    = Color.FromArgb(80, 180, 80);
    public static readonly Color Warning    = Color.FromArgb(220, 180, 60);
    public static readonly Color Error      = Color.FromArgb(220, 60, 60);

    public static readonly Font Title       = new("Segoe UI", 14f, FontStyle.Bold);
    public static readonly Font Header      = new("Segoe UI Semibold", 10f);
    public static readonly Font SectionHead = new("Segoe UI Semibold", 8.5f);
    public static readonly Font Normal      = new("Segoe UI", 9f);
    public static readonly Font Small       = new("Segoe UI", 8f);
    public static readonly Font Tiny        = new("Segoe UI", 7.5f);
    public static readonly Font Mono        = new("Cascadia Code", 8.5f);

    public static void ApplyTo(Control c)
    {
        c.BackColor = BgDeep;
        c.ForeColor = TextNorm;
        c.Font = Normal;
    }

    public static Panel MakeSeparator(DockStyle dock = DockStyle.Top)
    {
        return new Panel
        {
            Dock = dock,
            Height = dock is DockStyle.Top or DockStyle.Bottom ? 1 : 0,
            Width = dock is DockStyle.Left or DockStyle.Right ? 1 : 0,
            BackColor = Border
        };
    }
}
