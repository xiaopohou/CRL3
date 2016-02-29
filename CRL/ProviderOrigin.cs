﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Transactions;
using CRL.LambdaQuery;
namespace CRL
{
    /// <summary>
    /// 基本业务方法封装
    /// </summary>
    /// <typeparam name="TModel">源对象</typeparam>
    public abstract class ProviderOrigin<TModel>
        where TModel : IModel, new()
    {
        /// <summary>
        /// 基本业务方法封装
        /// </summary>
        public ProviderOrigin()
        {
            dbLocation = new DBLocation() { ManageType = GetType() };
        }
        /// <summary>
        /// 数据访问上下文
        /// </summary>
        /// <returns></returns>
        internal abstract DbContext GetDbContext();

        /// <summary>
        /// 当前数据访定位
        /// </summary>
        internal DBLocation dbLocation;

        /// <summary>
        /// 锁对象
        /// </summary>
        protected static object lockObj = new object();
        /// <summary>
        /// 对象被更新时,是否通知缓存服务器
        /// 在业务类中进行控制
        /// </summary>
        protected virtual bool OnUpdateNotifyCacheServer
        {
            get
            {
                return false;
            }
        }
        /// <summary>
        /// 创建当前类型查询表达式实列
        /// </summary>
        /// <returns></returns>
        public LambdaQuery<TModel> GetLambdaQuery()
        {
            var dbContext2 = GetDbContext();//避开事务控制,使用新的连接
            var query = new LambdaQuery<TModel>(dbContext2);
            return query;
        }

        #region 数据访问对象
        DBExtend _dBExtend;
        /// <summary>
        /// 数据访部对象
        /// 当前实例内只会创建一个,查询除外
        /// </summary>
        protected DBExtend DBExtend
        {
            get
            {
                if (_dBExtend == null)
                {
                    _dBExtend = GetDbHelper();
                }
                return _dBExtend;
            }
            set
            {
                _dBExtend = value;
            }
        }
        /// <summary>
        /// 数据访问对象[基本方法]
        /// 按指定的类型
        /// </summary>
        /// <returns></returns>
        protected DBExtend GetDbHelper(Type type = null)
        {
            var dbContext2 = GetDbContext();
            if (type != null)
            {
                dbLocation.ManageType = type;
            }
            var db = new DBExtend(dbContext2);
            if (dbLocation.ShardingDataBase == null)
            {
                db.OnUpdateNotifyCacheServer = OnUpdateNotifyCacheServer;
            }
            return db;
        }
        #endregion

        #region 创建结构
        /// <summary>
        /// 创建TABLE[基本方法]
        /// </summary>
        /// <returns></returns>
        public virtual string CreateTable()
        {
            DBExtend db = DBExtend;
            TModel obj1 = new TModel();
            var str = obj1.CreateTable(db);
            return str;
        }
        /// <summary>
        /// 创建表索引
        /// </summary>
        public void CreateTableIndex()
        {
            DBExtend db = DBExtend;
            TModel obj1 = new TModel();
            obj1.CheckIndexExists(db);
        }
        #endregion

        /// <summary>
        /// 写日志[基本方法]
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        public void Log(string message, string type = "CRL")
        {
            CoreHelper.EventLog.Log(message, type, false);
        }

        #region 添加
        /// <summary>
        /// 添加一条记录[基本方法]
        /// </summary>
        /// <param name="p"></param>
        public virtual void Add(TModel p)
        {
            DBExtend db = DBExtend;
            db.InsertFromObj(p);
        }
        /// <summary>
        /// 批量插入[基本方法]
        /// </summary>
        /// <param name="list"></param>
        /// <param name="keepIdentity">是否保持自增主键</param>
        public virtual void BatchInsert(List<TModel> list, bool keepIdentity = false)
        {
            DBExtend db = DBExtend;
            db.BatchInsert(list, keepIdentity);
        }
        #endregion

        #region 查询一项
        /// <summary>
        /// 按主键查询一项[基本方法]
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TModel QueryItem(object id)
        {
            DBExtend db = DBExtend;
            var lambda = Base.GetQueryIdExpression<TModel>(id);
            return QueryItem(lambda);
        }
        /// <summary>
        /// 按条件取单个记录[基本方法]
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="idDest">是否按主键倒序</param>
        /// <param name="compileSp"></param>
        /// <returns></returns>
        public TModel QueryItem(Expression<Func<TModel, bool>> expression, bool idDest = true, bool compileSp = false)
        {
            DBExtend db = DBExtend;
            return db.QueryItem<TModel>(expression, idDest, compileSp);
        }
        #endregion

        #region 删除
        /// <summary>
        /// 按主键删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int Delete(object id)
        {
            var db = DBExtend;
            var filed = TypeCache.GetTable(typeof(TModel)).PrimaryKey;
            string where = string.Format("{0}=@{0}", filed.Name);
            db.AddParam(filed.Name, id);
            return db.Delete<TModel>(where);
        }

        /// <summary>
        /// 按条件删除[基本方法]
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public int Delete(Expression<Func<TModel, bool>> expression)
        {
            DBExtend db = DBExtend;
            int n = db.Delete<TModel>(expression);
            return n;
        }
        #endregion

        #region 查询多项
        /// <summary>
        /// 返回全部结果[基本方法]
        /// </summary>
        /// <returns></returns>
        public List<TModel> QueryList()
        {
            DBExtend db = GetDbHelper();//避开事务控制,使用新的连接
            return db.QueryList<TModel>();
        }
        /// <summary>
        /// 指定条件查询[基本方法]
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="compileSp"></param>
        /// <returns></returns>
        public List<TModel> QueryList(Expression<Func<TModel, bool>> expression, bool compileSp = false)
        {
            DBExtend db = GetDbHelper();//避开事务控制,使用新的连接
            return db.QueryList<TModel>(expression, compileSp);
        }
        /**
        /// <summary>
        /// 指定条件查询[基本方法]
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<TModel> QueryList(LambdaQuery<TModel> query)
        {
            DBExtend db = GetDbHelper();//避开事务控制,使用新的连接
            return db.QueryList<TModel>(query);
        }
        /// <summary>
        /// 返回动态对象的查询
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<dynamic> QueryDynamic(LambdaQuery<TModel> query)
        {
            DBExtend db = DBExtend;
            return db.QueryDynamic<TModel>(query);
        }
        /// <summary>
        /// 返回指定类型
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<TResult> QueryDynamic<TResult>(LambdaQuery<TModel> query) where TResult : class,new()
        {
            DBExtend db = DBExtend;
            return db.QueryDynamic<TModel, TResult>(query);
        }
        **/
        #endregion

        #region 分页
      
        /**
        /// <summary>
        /// 分页,并返回当前类型
        /// 会按GROUP和自动编译判断
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<TModel> Page(CRL.LambdaQuery<TModel> query)
        {
            DBExtend db = DBExtend;
            return db.Page<TModel, TModel>(query);
        }
        /// <summary>
        /// 分页,并返回指定类型
        /// 会按GROUP和自动编译判断
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<TResult> Page<TResult>(CRL.LambdaQuery<TModel> query) where TResult : class,new()
        {
            DBExtend db = DBExtend;
            return db.Page<TModel, TResult>(query);
        }

        /// <summary>
        /// 返回动态类型分页
        /// 会按GROUP和自动编译判断
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<dynamic> PageDynamic(CRL.LambdaQuery<TModel> query)
        {
            DBExtend db = DBExtend;
            return db.Page(query);
        }
         * */
        #endregion

        #region 更新
        /// <summary>
        /// 按对象差异更新,对象需由查询创建[基本方法]
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int Update(TModel item)
        {
            DBExtend db = DBExtend;
            return db.Update(item);
        }

        /// <summary>
        /// 指定条件并按对象差异更新[基本方法]
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public int Update(Expression<Func<TModel, bool>> expression, TModel model)
        {
            DBExtend db = DBExtend;
            int n = db.Update<TModel>(expression, model);
            return n;
        }
        /// <summary>
        /// 指定条件和参数进行更新[基本方法]
        /// </summary>
        /// <param name="expression">条件</param>
        /// <param name="setValue">值</param>
        /// <returns></returns>
        public int Update(Expression<Func<TModel, bool>> expression, ParameCollection setValue)
        {
            DBExtend db = DBExtend;
            int n = db.Update<TModel>(expression, setValue);
            return n;
        }
        /// <summary>
        /// 按匿名对象更新
        /// </summary>
        /// <typeparam name="TOjbect"></typeparam>
        /// <param name="expression"></param>
        /// <param name="updateValue"></param>
        /// <returns></returns>
        public int Update<TOjbect>(Expression<Func<TModel, bool>> expression, TOjbect updateValue) where TOjbect : class
        {
            var db = DBExtend;
            int n = db.Update<TModel>(expression, updateValue);
            return n;
        }
        #endregion

        #region 格式化命令查询

        /// <summary>
        /// 指定格式化查询列表[基本方法]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="parame"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        protected List<T> ExecListWithFormat<T>(string sql, ParameCollection parame, params Type[] types) where T : class, new()
        {
            DBExtend db = DBExtend;
            foreach (var p in parame)
            {
                db.AddParam(p.Key, p.Value);
            }
            return db.ExecList<T>(sql, types);
        }
        /// <summary>
        /// 指定格式化更新[基本方法]
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parame"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        protected int ExecuteWithFormat(string sql, ParameCollection parame, params Type[] types)
        {
            DBExtend db = DBExtend;
            foreach (var p in parame)
            {
                db.AddParam(p.Key, p.Value);
            }
            return db.Execute(sql, types);
        }
        /// <summary>
        /// 指定格式化返回单个结果[基本方法]
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parame"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        protected T ExecScalarWithFormat<T>(string sql, ParameCollection parame, params Type[] types)
        {
            DBExtend db = DBExtend;
            foreach (var p in parame)
            {
                db.AddParam(p.Key, p.Value);
            }
            return db.ExecScalar<T>(sql, types);
        }

        #endregion

        #region 导入导出
        /// <summary>
        /// 导出为json[基本方法]
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public string ExportToJson(Expression<Func<TModel, bool>> expression)
        {
            var list = QueryList(expression);
            var xml = CoreHelper.SerializeHelper.SerializerToJson(list, Encoding.UTF8);
            return xml;
        }
        /// <summary>
        /// 从json导入[基本方法]
        /// </summary>
        /// <param name="json"></param>
        /// <param name="delExpression">要删除的数据</param>
        /// <param name="keepIdentity">是否保留自增主键</param>
        /// <returns></returns>
        public int ImportFromJson(string json, Expression<Func<TModel, bool>> delExpression, bool keepIdentity = false)
        {
            var obj = CoreHelper.SerializeHelper.SerializerFromJSON(json, typeof(List<TModel>), Encoding.UTF8) as List<TModel>;
            Delete(delExpression);
            BatchInsert(obj, keepIdentity);
            return obj.Count;
        }
        #endregion

        #region 统计
        /// <summary>
        /// 统计[基本方法]
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="compileSp"></param>
        /// <returns></returns>
        public int Count(Expression<Func<TModel, bool>> expression, bool compileSp = false)
        {
            DBExtend db = GetDbHelper();//避开事务控制,使用新的连接
            return db.Count<TModel>(expression, compileSp);
        }
        /// <summary>
        /// sum 按表达式指定字段[基本方法]
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="expression"></param>
        /// <param name="field"></param>
        /// <param name="compileSp"></param>
        /// <returns></returns>
        public TType Sum<TType>(Expression<Func<TModel, bool>> expression, Expression<Func<TModel, TType>> field, bool compileSp = false)
        {
            DBExtend db = GetDbHelper();//避开事务控制,使用新的连接
            return db.Sum<TType, TModel>(expression, field, compileSp);
        }
        /// <summary>
        /// 取最大值[基本方法]
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="expression"></param>
        /// <param name="field"></param>
        /// <param name="compileSp"></param>
        /// <returns></returns>
        public TType Max<TType>(Expression<Func<TModel, bool>> expression, Expression<Func<TModel, TType>> field, bool compileSp = false)
        {
            DBExtend db = GetDbHelper();//避开事务控制,使用新的连接
            return db.Max<TType, TModel>(expression, field, compileSp);
        }
        /// <summary>
        /// 取最小值[基本方法]
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="expression"></param>
        /// <param name="field"></param>
        /// <param name="compileSp"></param>
        /// <returns></returns>
        public TType Min<TType>(Expression<Func<TModel, bool>> expression, Expression<Func<TModel, TType>> field, bool compileSp = false)
        {
            DBExtend db = GetDbHelper();//避开事务控制,使用新的连接
            return db.Min<TType, TModel>(expression, field, compileSp);
        }
        #endregion

        #region 包装为事务执行
        /// <summary>
        /// 使用DbTransaction封装事务(不推荐)
        /// </summary>
        /// <param name="db"></param>
        /// <param name="method"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool PackageTrans2(DBExtend db, TransMethod method, out string error)
        {
            error = "";
            db.BeginTran();
            try
            {
                var a = method(out error);
                if (!a)
                {
                    db.RollbackTran();
                    return false;
                }
                db.CommitTran();
            }
            catch (Exception ero)
            {
                error = "提交事务时发生错误:" + ero.Message;
                db.RollbackTran();
                return false;
            }
            return true;
        }
        /// <summary>
        /// 使用TransactionScope封装事务[基本方法]
        /// </summary>
        /// <param name="method"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool PackageTrans(TransMethod method, out string error)
        {
            error = "";
            using (var trans = new TransactionScope())
            {
                try
                {
                    var a = method(out error);
                    if (!a)
                    {
                        return false;
                    }
                    trans.Complete();
                }
                catch (Exception ero)
                {
                    error = "提交事务时发生错误:" + ero.Message;
                    return false;
                }
            }
            return true;
        }
        #endregion
    }
}
