using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Core.Enums
{

    public enum PageEnums
    {
         
        [Description("主页")]
        PlaylistPage = 0,
        [Description("歌单")]
        HeartPage =1,
        [Description("设置")]
        SettingsPage = 5,
        [Description("歌手")]
        SingerPage=2,
        [Description("专辑")]
        AlbumPage =3,
        [Description("播放")]
        PlayerPage =4,

    }
}
