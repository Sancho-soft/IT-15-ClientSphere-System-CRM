$connString = "Data Source=LAPTOP-PCKVQMPF\SQLEXPRESS;Initial Catalog=db_clientsphere;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
$connection = New-Object System.Data.SqlClient.SqlConnection($connString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = "UPDATE AspNetUsers SET TwoFactorEnabled = 1 WHERE Email = 'superadmin@clientsphere.com'"
$rows = $command.ExecuteNonQuery()
Write-Host "Updated $rows row(s). TwoFactorEnabled is now enabled for superadmin@clientsphere.com."
$connection.Close()
