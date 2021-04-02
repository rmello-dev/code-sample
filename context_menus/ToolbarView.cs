using UnityEngine;

using Game.Lib.Media;
using Game.Api.Pooling;
using Game.Api.Rendering;
using Game.Lib.Interface;

namespace Game.Api.Interface
{

/// <summary>
/// Presenter for toolbar panels.
/// </summary>
public sealed class ToolbarView
    {
    // --- Inner Members:

    /// <summary>
    /// Enumeration of toolbar models.
    /// Defines toolbar size and content.
    /// </summary>
    public enum Model
        {
        Taskbar,        // creates taskbar with tooltip
        Controlbar,     // creates control bar
        }

    // --- Common Fields:

    private readonly GameObject viewRoot;                   // hierarchy root

    private readonly Model toolbarModel;                    // content model flag                         

    private readonly Proxy<SpriteRender> baseBar;           // toolbar image

    private readonly Proxy<ScriptRender> tooltipLabel;      // text display for hover tooltips
    private readonly Color tooltipColor;                    // theme color for tooltip text
    private readonly Color tooltipContrast;                 // theme color for tooltip shadow

    private readonly ButtonRay[] buttonSet;                 // array of sprite buttons 
    
        // taskbar fields:

    public Context.Task.Group TaskContext { get; private set; }                 // tracks context currently on display

    private const int task_HeadButton = (byte)Context.Task.Set.Index.A;         // taskbar button set iterator head = 0
    private const int task_TailButton = (byte)Context.Task.Set.Index.H;         // taskbar button set iterator tail = 7

        // controlbar fields:

    public Context.Control.Group ControlContext { get; private set; }           // tracks context currently on display

    private const int ctrl_HeadButton = (byte)Context.Control.Set.Index.A;      // controlbar button set iterator head = 0 
    private const int ctrl_TailButton = (byte)Context.Control.Set.Index.D;      // controlbar button set iterator tail = 3

    // --- Initialization:

    public ToolbarView(GameObject hierarchyRoot, Model model)
        {
        // setup - model version

        bool task = (model == Model.Taskbar);

        toolbarModel = model;

        // setup - theme assets

        Themes.Theme theme = MasterController.Display.Screen.InterfaceTheme;

        Sprite toolbarBase;

        if (task) { InterfaceTheme.TaskbarDecorator(theme, out toolbarBase, out tooltipColor, out tooltipContrast); }
        else { InterfaceTheme.ControlbarDecorator(theme, out toolbarBase, out tooltipColor, out tooltipContrast); }
    
        // setup - hierarchy root

        if (task) { viewRoot = new GameObject(ResourceLabels.gui_Taskbar); }
        else { viewRoot = new GameObject(ResourceLabels.gui_Controlbar); }

        viewRoot.layer = SceneAdapter.GetLayerIndex(SceneAdapter.LayerTag.Virtual);       
        viewRoot.transform.SetParent(hierarchyRoot.transform);

        // setup - panel base

        baseBar = new Proxy<SpriteRender>(MasterController.Display.Render.Pool_Sprites);
        baseBar.Entity.SetHierarchy(viewRoot);
        baseBar.Entity.SetRender(toolbarBase);

            baseBar.Entity.SetLayer(LayerOrder.gui_BaseCt);

            float baseOffsetX = (task) ? (InterfaceLayout.Toolbar.Task_Base_OffsetX):(InterfaceLayout.Toolbar.Ctrl_Base_OffsetX);
            float baseOffsetY = (task) ? (InterfaceLayout.Toolbar.Task_Base_OffsetY):(InterfaceLayout.Toolbar.Ctrl_Base_OffsetY);

        // setup - tooltip 

        if (task)
           {
           tooltipLabel = new Proxy<ScriptRender>(MasterController.Display.Render.Pool_Scripts);
           tooltipLabel.Entity.SetHierarchy(viewRoot);

           tooltipLabel.Entity.SetLayer(LayerOrder.gui_BaseEl);
           tooltipLabel.Entity.UpdateColors(tooltipColor,tooltipContrast);
           tooltipLabel.Entity.UpdateLayout(new TextScheme.Layout(Placement.LeftMiddle));

               float tooltipOffsetX = InterfaceLayout.Toolbar.Task_Tooltip_OffsetX;
               float tooltipOffsetY = InterfaceLayout.Toolbar.Task_Tooltip_OffsetY;
               Vector2 tooltipOffset = new Vector2(tooltipOffsetX,tooltipOffsetY);

               tooltipLabel.Entity.SetPosition(tooltipOffset);

               float tooltipScale = InterfaceLayout.Toolbar.Task_Tooltip_Scale;
               tooltipLabel.Entity.SetScale(tooltipScale);

           ChangeTooltip(string.Empty);
           }

        // setup - buttons

        int tail = (task) ? (task_TailButton):(ctrl_TailButton);
        int head = (task) ? (task_HeadButton):(ctrl_HeadButton);

        buttonSet = new ButtonRay[tail+1];

        for (int index = head; index <= tail; index++)
            {
            buttonSet[index] = new ButtonRay(theme);
            buttonSet[index].SetRoot(viewRoot);
            }

        float yOffset = (task) ? (InterfaceLayout.Toolbar.Task_Buttons_OffsetY):(InterfaceLayout.Toolbar.Ctrl_Buttons_OffsetY);
        float xOrigin = (task) ? (InterfaceLayout.Toolbar.Task_Buttons_OriginX):(InterfaceLayout.Toolbar.Ctrl_Buttons_OriginX);
        float xGap = InterfaceLayout.Toolbar.Common_Button_GapX;

        for (int index = head; index <= tail; index++)
            {

            InterfaceLayout.SetRelativePosition(buttonSet[index],xOrigin+(index*xGap),yOffset);
            }

        // base layout

        InterfaceLayout.SetRelativePosition(viewRoot,baseOffsetX,baseOffsetY);

        // initial state

        bool regionFlag = (MasterController.Game.MapView);

        if (task) { if (!regionFlag) { SwapContext(Context.Task.Group.Initial); } else { SwapContext(Context.Task.Group.Map_Initial); } }
        else { if (!regionFlag) { SwapContext(Context.Control.Group.Initial); } else { SwapContext(Context.Control.Group.Map_Initial); } }
        }

    // --- External Behaviors:

    /// <summary>
    /// Changes taskbar tooltip message.
    /// Uses empty string to erase display.
    /// </summary>
    public void ChangeTooltip(string tooltip)
        {
        if (toolbarModel == Model.Taskbar) { tooltipLabel.Entity.Text = TextData.Plain(tooltip); }
        else { MasterController.Display.Screen.LinkTaskbar.ChangeTooltip(tooltip); }                    // FIX: works from controlbar
        }

    /// <summary>
    /// Swaps taskbar context and replace button options.
    /// </summary>
    public void SwapContext(Context.Task.Group contextGroup)
        {
        Context.Task.Set options = Context.Task.OptionsFromGroup(contextGroup,false);

        for(int index = task_HeadButton; index <= task_TailButton; index++)
            {
            Context.Task.Option option = options.ByIndex(index);

            setOption(buttonSet[index],index,option);
            }

        TaskContext = contextGroup;
        }

    /// <summary>
    /// Swaps controlbar context and replace button options.
    /// </summary>
    public void SwapContext(Context.Control.Group contextGroup)
        {
        Context.Control.Set options = Context.Control.OptionsFromGroup(contextGroup);

        for(int index = ctrl_HeadButton; index <= ctrl_TailButton; index++)
            {
            Context.Control.Option option = options.ByIndex(index);

            setOption(buttonSet[index],option);
            }

        ControlContext = contextGroup;
        }

    /// <summary>
    /// Ensures control bar time context displays appropriate pause/resume option.
    /// </summary>
    public void UpdateTimeControl()
        {
        if (toolbarModel == Model.Controlbar)
           {
           bool timeFlow = (MasterController.Game.World.PlaySpeed != Lib.World.WorldModel.TimeStep.Paused);
           bool timeContext = (ControlContext == Context.Control.Group.Time || ControlContext == Context.Control.Group.Map_Time);

           if (timeContext)
              {
              Context.Control.Option action = (timeFlow) ? (Context.Control.Option.L2_TimePause):(Context.Control.Option.L2_TimeResume);

              setOption(buttonSet[(int)Context.Control.Set.Index.A],action);
              }
           }
        }

    // --- Internal Behaviors:

    /// <summary>
    /// Defines task button icon, tooltip, shortcut, and click action.
    /// </summary>
    private void setOption(ButtonRay button, int optionIndex, Context.Task.Option option) 
        {
        button.SetSymbol(Context.Task.SymbolFromOption(option));
        button.SetAction(Context.Task.ActionFromOption(option));
        button.SetTooltip(Context.Task.TooltipFromOption(option));
        button.SetShortcut(Context.Task.ShortcutFromOption(option,optionIndex));

        button.SetInteraction((option != Context.Task.Option.L0_Blank));
        }
    
    /// <summary>
    /// Defines control button icon, tooltip, shortcut, and click action.
    /// </summary>
    private void setOption(ButtonRay button, Context.Control.Option option) 
        {
        button.SetSymbol(Context.Control.SymbolFromOption(option));
        button.SetAction(Context.Control.ActionFromOption(option));
        button.SetTooltip(Context.Control.TooltipFromOption(option));
        button.SetShortcut(Context.Control.ShortcutFromOption(option));

        button.SetInteraction((option != Context.Control.Option.L0_Blank));
        }

    }


}
