# top5-minimal

CRUD completo REST na base top5-mysql, com frontend e backend, para o Dev Web. Esta versão foi feita usando o estilo _Minimal Apis_.

Há também uma [versão usando controllers tradicionais](https://github.com/ermogenes/top5).

## Notas

Subir o banco rapidamente com Docker (MySQL 8.0.23):

```
docker run -p 3333:3306 -e MYSQL_ROOT_PASSWORD=xxx ermogenes/top5-mysql
```

Ou então assim, e faça a carga da [estrutura e dados](https://github.com/ermogenes/top5-mysql/blob/master/scripts/top5.sql) manualmente  (MySQL 8.0.28):

```
docker run -p 3333:3306 -e MYSQL_ROOT_PASSWORD=xxx mysql:8.0.28
```

Foi utilizada a lib [Pomelo](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql) `Pomelo.EntityFrameworkCore.MySql` em vez do Connector/NET oficial da Oracle, devido ao suporte simplificado a diferentes versões do MySQL.

Comandos utilizados para fazer o _scaffolding_:

```
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef

dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Pomelo.EntityFrameworkCore.MySql

dotnet ef dbcontext scaffold "server=localhost;port=3333;uid=root;pwd=xxx;database=top5" Pomelo.EntityFrameworkCore.MySql -o db -f --no-pluralize
```
