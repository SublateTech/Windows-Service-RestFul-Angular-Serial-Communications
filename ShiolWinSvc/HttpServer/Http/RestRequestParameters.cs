using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace m.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    public class RestRequestParameters
    {
        public RestRequestParameters()
        {
            //only in here thank you
            m_params = new Dictionary<string, string>();
        }
        public string this[string key]
        {
            get
            {
                string param;

                if (!m_params.TryGetValue(key, out param))
                    return null;

                return param;
            }
            set
            {
                m_params.Add(key, value);
            }
        }

        private IDictionary<string, string> m_params;

        public IDictionary<string, string> Items { get { return m_params; } }
    }
}


