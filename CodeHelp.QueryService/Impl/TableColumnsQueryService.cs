﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CodeHelp.Common.Exceptions;
using CodeHelp.Common.Mapper;
using CodeHelp.Data.Dapper;
using CodeHelp.Domain;
using CodeHelp.QueryService.ViewModels;

namespace CodeHelp.QueryService.Impl
{
    public class TableColumnsQueryService : ITableColumnsQueryService
    {
        private readonly ISqlDatabaseProxy _sqlDatabaseProxy;
        private readonly IMap _map;

        public TableColumnsQueryService(ISqlDatabaseProxy sqlDatabaseProxy, 
            IMap map)
        {
            _sqlDatabaseProxy = sqlDatabaseProxy;
            _map = map;
        }

        public async Task<TableColumnsListPaginationViewModel> Get(string tableName)
        {
            var whereSql = "";
            if (!string.IsNullOrEmpty(tableName))
            {
                whereSql += "WHERE sysobjects.name LIKE @tableName";
            }
            var sql = @"SELECT 
                        sysobjects.name TableName,
                        syscolumns.colid ColumnColid,
                        syscolumns.name ColumnName,
                        syscolumns.length ColumnLength,
                        systypes.name ColumnType,
                        sys.extended_properties.value [Description],
                        syscolumns.isnullable [IsNull],
                        syscomments.text DefaultValue,
                        (CASE WHEN EXISTS(SELECT 1 FROM sysobjects
                        JOIN sysindexes ON sysindexes.name = sysobjects.name  
                        JOIN sysindexkeys ON sysindexes.id = sysindexkeys.id AND sysindexes.indid = sysindexkeys.indid 
                        WHERE xtype='PK' AND parent_obj = syscolumns.id    
                        AND sysindexkeys.colid = syscolumns.colid) THEN 1 ELSE 0 END) AS [IsPrimaryKey],
                        (Case syscolumns.status WHEN 128 THEN 1 ELSE 0 END) AS [IsIdentity],
                        syscolumns.scale [Scale]
                        FROM syscolumns   
                        JOIN systypes ON syscolumns.xtype=systypes.xtype AND systypes.name <> 'sysname' 
                        LEFT JOIN sys.extended_properties ON sys.extended_properties.minor_id = syscolumns.colid AND sys.extended_properties.major_id = syscolumns.id
                        LEFT JOIN sysobjects ON sysobjects.id =syscolumns.id 
                        LEFT JOIN syscomments ON syscolumns.cdefault=syscomments.id "
                        + (!string.IsNullOrEmpty(whereSql) ? $"{whereSql}" : "") 
                        + " ORDER BY ColumnColid";
            try
            {
                var result = await _sqlDatabaseProxy.Query<TableColumns>(sql, new
                {
                    tableName = $"{tableName}"
                });
                var results= _map.Map<List<TableColumnsListViewModel>>(result);
                return new TableColumnsListPaginationViewModel(results);
            }
            catch (SqlException sqlException)
            {
                throw DataException.DatabaseError(sqlException);
            }
            catch (Exception exception)
            {
                throw DataException.GeneralError(exception);
            }
        }
    }
}