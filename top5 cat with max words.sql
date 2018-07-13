SELECT TOP 5
CAST(Category AS NVARCHAR(100)) Category,
SUM((DATALENGTH(Referats.Data) - DATALENGTH(cast(replace(cast(Referats.Data as nvarchar(max)), ' ', '') as ntext))) / 2) as cnt
FROM Referats
GROUP BY CAST(Category AS NVARCHAR(100))
ORDER BY cnt DESC;