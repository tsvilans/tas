/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017-2018 Tom Svilans
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

using tas.Core;
using tas.Lam;
using System.Runtime.Remoting;

namespace tas.ESR2
{

    #region Model and condition types

    public abstract class Condition
    {
        public abstract bool Evaluate();
    }

    public class ConditionList
    {
        List<Condition> _conditions = new List<Condition>();

        public Condition this[int i]
        {
            get { return _conditions[i]; }
            set { _conditions[i] = value; }
        }

        public void Add(Condition c) => _conditions.Add(c);
        public void Remove(Condition c) => _conditions.Remove(c);
        public void RemoveAt(int i) => _conditions.RemoveAt(i);
        public void Clear() => _conditions.Clear();

        public bool Evaluate()
        {
            bool yep = true;

            for (int i = 0; i < _conditions.Count; ++i)
                if (!_conditions[i].Evaluate())
                    yep = false;

            return yep;
        }
    }

    public abstract class Model
    {
        public Dictionary<string, Param> Inputs;
        public Dictionary<string, Param> Outputs;

        public bool Resolved { get; protected set; }

        public ConditionList Conditions;

        public Model()
        {
            Inputs = new Dictionary<string, Param>();
            Outputs = new Dictionary<string, Param>();
            Conditions = new ConditionList();
            Resolved = false;
        }

        public abstract bool Evaluate();

    }

    public class Param
    {
        public Guid Id {get; set;}
        dynamic _value;
        public object Value
        {
            get { return Convert.ChangeType(_value, _value.GetType()); }
            set
            {
                //if (!Locked && ((ObjectHandle)value).Unwrap().GetType() == ((ObjectHandle)_value).Unwrap().GetType())
                if (!Locked && value.GetType() == _value.GetType())
                        _value = value;
                else
                    throw new ParamLockedException("Param is locked or using incompatible types!"
                        + "\n   Param type is " + _value.GetType().ToString()
                        + "\n   Input type is " + value.GetType().ToString());
            }
        }

        dynamic _proposed;
        public dynamic Proposed
        {
            get { return _proposed; }
            set
            {
                if (value.GetType() == _value.GetType())
                    throw new ParamLockedException("Param is locked or using incompatible types!"
                    + "\n   Param type is " + ((ObjectHandle)_value).Unwrap().GetType().ToString()
                    + "\n   Input type is " + ((ObjectHandle)value).Unwrap().GetType().ToString()
                    );

                _proposed = value;
            }
        }

        public bool Locked;

        public Param(dynamic data)
        {
            Id = Guid.NewGuid();
            _value = data;
        }

        public Param(dynamic data, Guid id)
        {
            if (id == Guid.Empty)
                Id = Guid.NewGuid();
            else
                Id = id;

            _value = data;
        }

        public bool Accept()
        {
            try
            {
                Value = Proposed;
            }
            catch (ParamLockedException pe)
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is Param)
                //if (((ObjectHandle)(obj as Param).Value).Unwrap().GetType() == ((ObjectHandle)_value).Unwrap().GetType() &&
                if ((obj as Param).Value.GetType() == _value.GetType() &&
                        (obj as Param).Id == Id)
                    return true;
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            //return "Param." + ((ObjectHandle)_value).Unwrap().GetType().ToString();
            return "Param(" + _value.GetType().ToString() + ")";
        }
    }

    public class Param<T>
    {
        public Guid Id { get; set; }
        T _value;
        public T Value
        {
            get { return _value; }
            set
            {
                if (!Locked && value.GetType() == _value.GetType())
                    _value = value;
                else
                    throw new ParamLockedException("Param is locked or using incompatible types!"
                        + "\n   Param type is " + _value.GetType().ToString()
                        + "\n   Input type is " + value.GetType().ToString());
            }
        }

        T _proposed;
        public T Proposed
        {
            get { return _proposed; }
            set
            {
                if (value.GetType() == _value.GetType())
                    throw new ParamLockedException("Param is locked or using incompatible types!"
                    + "\n   Param type is " + _value.GetType().ToString()
                    + "\n   Input type is " + value.GetType().ToString()
                    );

                _proposed = value;
            }
        }

        public bool Locked;

        public Param(T data)
        {
            Id = Guid.NewGuid();
            _value = data;
        }

        public Param(dynamic data, Guid id)
        {
            if (id == Guid.Empty)
                Id = Guid.NewGuid();
            else
                Id = id;

            _value = data;
        }

        public bool Accept()
        {
            try
            {
                Value = Proposed;
            }
            catch (ParamLockedException pe)
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is Param<T>)
                if ((obj as Param<T>).Value.GetType() == _value.GetType() &&
                        (obj as Param<T>).Id == Id)
                    return true;
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            //return "Param." + ((ObjectHandle)_value).Unwrap().GetType().ToString();
            return "Param(" + _value.GetType().ToString() + ")";
        }
    }

    public class ParamLockedException : Exception
    {
        public ParamLockedException()
        {
        }

        public ParamLockedException(string message)
            : base(message)
        {
        }

        public ParamLockedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class ParamIncompatibleException : Exception
    {
        public ParamIncompatibleException()
        {
        }

        public ParamIncompatibleException(string message)
            : base(message)
        {
        }

        public ParamIncompatibleException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    #endregion

    #region Custom model types

    public class BlankModel : Model
    {
        public BlankModel(Param g_param)
        {
            if (!(g_param.Value is Glulam))
                throw new ParamIncompatibleException("Param is not a Glulam type!");

            Inputs.Add("blank", g_param);

            Param lh_param = new Param((g_param.Value as Glulam).Data.LamHeight);
            Param lw_param = new Param((g_param.Value as Glulam).Data.LamWidth);

            Outputs.Add("lam_height", lh_param);
            Outputs.Add("lam_width", lw_param);

            Condition_BlankBendingLimits con_bbl = new Condition_BlankBendingLimits(g_param, lh_param, lw_param);
            Conditions.Add(con_bbl);

        }
        public override bool Evaluate()
        {
            Resolved = Conditions.Evaluate();
            return Resolved;
        }
    }

    #endregion

    #region Custom condition types

    public class Condition_BeamContinuity : Condition
    {
        Param Pos1, Tan1, Pos2, Tan2;
        Param PosTolerance, TanTolerance;

        public Condition_BeamContinuity(Param pos1, Param tan1, Param pos2, Param tan2, Param pos_tolerance, Param tan_tolerance)
        {
            if (pos1.GetType() != typeof(Point3d))
                throw new ParamIncompatibleException("pos1 must be a Point3d.");
            if (tan1.GetType() != typeof(Vector3d))
                throw new ParamIncompatibleException("tan1 must be a Point3d.");
            if (pos2.GetType() != typeof(Point3d))
                throw new ParamIncompatibleException("pos2 must be a Point3d.");
            if (tan2.GetType() != typeof(Vector3d))
                throw new ParamIncompatibleException("tan2 must be a Point3d.");
            if (pos_tolerance.GetType() != typeof(double))
                throw new ParamIncompatibleException("pos_tolerance must be a Point3d.");
            if (tan_tolerance.GetType() != typeof(double))
                throw new ParamIncompatibleException("tan_tolerance must be a Point3d.");

            Pos1 = pos1;
            Tan1 = tan1;
            Pos2 = pos2;
            Tan2 = tan2;

            PosTolerance = pos_tolerance;
            TanTolerance = tan_tolerance;
        }

        public override bool Evaluate()
        {
            bool bad = false;

            if (((Point3d)Pos1.Value).DistanceTo((Point3d)Pos2.Value) > (double)PosTolerance.Value)
                bad = true;

            if (((Vector3d)Tan1.Value) * ((Vector3d)Tan2.Value) + 1 > (double)TanTolerance.Value)
                bad = true;

            return !bad;
        }
    }

    //public class Condition_

    public class Condition_BlankBendingLimits : Condition
    {
        Param GlulamParam;
        Param LHParam;
        Param LWParam;

        public Condition_BlankBendingLimits(Param glulam_param, Param lh_param, Param lw_param)
        {
            GlulamParam = glulam_param;
            LHParam = lh_param;
            LWParam = lw_param;
        }

        public override bool Evaluate()
        {
            Glulam g = GlulamParam.Value as Glulam;

            bool is_ok_x = false, is_ok_y = false;

            Glulam gdup = g.Duplicate();
            gdup.Data.LamHeight = (double)LHParam.Value;
            gdup.Data.LamWidth = (double)LWParam.Value;

            bool res = gdup.InKLimitsComponent(out is_ok_x, out is_ok_y);

            if (res)
                return res;
            else
            {
                if (!LHParam.Locked && !LWParam.Locked)
                {
                    GlulamData gdata = GlulamData.FromCurveLimits(g.Centreline, g.GetAllPlanes());
                    LHParam.Value = gdata.LamHeight;
                    LWParam.Value = gdata.LamWidth;
                    return true;
                }
                else
                {
                    // TODO: Propose new relaxed curve
                    return false;
                }
            }
        }

    }

    #endregion
}
