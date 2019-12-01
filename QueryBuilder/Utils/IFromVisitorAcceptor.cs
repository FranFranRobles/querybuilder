using System;
using System.Collections.Generic;
using System.Text;

namespace SqlKata.Utils
{
    interface IFromVisitorAcceptor
    {
        void Accept(IFromVisitor visitor);
    }
}
