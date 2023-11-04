using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yonmoku-WPF
{
    interface IGame : IDisposable
    {
        public void Initialize();
        public void OnHit();
        public void OnClick();
    }
}
