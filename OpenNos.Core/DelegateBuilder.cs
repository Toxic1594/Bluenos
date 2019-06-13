﻿/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OpenNos.Core
{
    public static class DelegateBuilder
    {
        #region Methods

        public static T BuildDelegate<T>(MethodInfo method, params object[] missingParamValues)
        {
            Queue<object> queueMissingParams = new Queue<object>(missingParamValues);
            MethodInfo dgtMi = typeof(T).GetMethod("Invoke");
            ParameterInfo[] dgtParams = dgtMi.GetParameters();
            ParameterExpression[] paramsOfDelegate = dgtParams.Select(tp => Expression.Parameter(tp.ParameterType, tp.Name)).ToArray();
            ParameterInfo[] methodParams = method.GetParameters();
            if (method.IsStatic)
            {
                Expression[] paramsToPass = methodParams.Select((p, i) => CreateParam(paramsOfDelegate, i, p, queueMissingParams)).ToArray();
                Expression<T> expr = Expression.Lambda<T>(Expression.Call(method, paramsToPass), paramsOfDelegate);
                return expr.Compile();
            }
            else
            {
                UnaryExpression paramThis = Expression.Convert(paramsOfDelegate[0], method.DeclaringType);
                Expression[] paramsToPass = methodParams.Select((p, i) => CreateParam(paramsOfDelegate, i + 1, p, queueMissingParams)).ToArray();
                Expression<T> expr = Expression.Lambda<T>(Expression.Call(paramThis, method, paramsToPass), paramsOfDelegate);
                return expr.Compile();
            }
        }

        private static Expression CreateParam(ParameterExpression[] paramsOfDelegate, int index, ParameterInfo callParamType, Queue<object> queueMissingParams)
        {
            if (index < paramsOfDelegate.Length)
            {
                return Expression.Convert(paramsOfDelegate[index], callParamType.ParameterType);
            }

            if (queueMissingParams.Count > 0)
            {
                return Expression.Constant(queueMissingParams.Dequeue());
            }

            if (callParamType.ParameterType.IsValueType)
            {
                return Expression.Constant(Activator.CreateInstance(callParamType.ParameterType));
            }

            return Expression.Constant(null);
        }

        #endregion
    }
}