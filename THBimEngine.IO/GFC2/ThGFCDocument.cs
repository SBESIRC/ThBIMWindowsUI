using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using THBimEngine.Domain;
using glodon.objectbufnet;

namespace THBimEngine.IO.GFC2
{

    public class ThGFCDocument
    {
        private Writer gfcWriter;
        private int m_id;

        public Dictionary<string, int> CommonStr;
        public Dictionary<Tuple<double, double>, int> CommonVector2d;
        public Dictionary<Tuple<double, double, double>, int> CommonVector3d;

        public ThGFCDocument()
        {
            gfcWriter = new Writer();
            m_id = 0;
            CommonStr = new Dictionary<string, int>();
        }

        public static ThGFCDocument Create(string filePath)
        {
            var doc = new ThGFCDocument();
            doc.Open(filePath, false, "");
            return doc;
        }

        public bool Open(string sFileName, bool bIsBinary, string sProductCode)
        {
            this.gfcWriter.open(sFileName, bIsBinary, sProductCode);
            var flag = true;

            return flag;
        }

        public int writeEntity(Entity entity)
        {
            int num;
            int id = -1;

            if (entity != null)
            {
                if (!(entity is NGfc2Object))
                {
                    id = this.gfcWriter.writeEntity(entity);
                }
                else
                {
                    this.m_id++;
                    id = this.gfcWriter.writeEntity(entity);
                }
                num = id;
            }
            else
            {
                num = id;
            }
            return num;
        }

        public void Close()
        {
            this.gfcWriter.close();
        }
    }
}
