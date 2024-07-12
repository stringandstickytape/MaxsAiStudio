using FontAwesome.Sharp;

namespace AiTool3
{
    internal static class ButtonIconHelper
    {

        internal static void SetButtonIcon(IconChar iconChar, Button button)
        {
            button.ImageAlign = ContentAlignment.TopCenter;
            button.TextImageRelation = TextImageRelation.ImageAboveText;
            button.Image = iconChar.ToBitmap(Color.White, 48);
            //button.Text = "";
        }
    }
}