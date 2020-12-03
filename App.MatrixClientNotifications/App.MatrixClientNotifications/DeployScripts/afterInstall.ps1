$rootDir = "C:\BrightMLS\MatrixClientNotifications\bin\Release"

if(Test-Path "$rootDir\BrightMls.Enterprise.MatrixClientNotifications.exe" -PathType Leaf){
    Remove-Item -path "$rootDir\BrightMls.Enterprise.MatrixClientNotifications.exe"
}

$deploymentGroupAppName = "matrixclientnotificationsjob"
$parameterStoreAppName = "matrixclientnotifications"
switch ( $env:DEPLOYMENT_GROUP_NAME )
{
    "aue1d1z1cdg${deploymentGroupAppName}developdeploy" {
        Rename-Item -path "$rootDir\app.dev.config" -NewName BrightMls.Enterprise.MatrixClientNotifications.exe -Force
        Get-SSMParameter -WithDecryption 1 -Name "/secure/aue1/d1/dotnetiis/${parameterStoreAppName}/connectionstrings" | Select -ExpandProperty Value | Out-File -FilePath "$rootDir\ConnectionStrings.config"
    }
    "auw2d1z1cdg${deploymentGroupAppName}developdeploy" {
        Rename-Item -path "$rootDir\app.dev.config" -NewName BrightMls.Enterprise.MatrixClientNotifications.exe -Force
        Get-SSMParameter -WithDecryption 1 -Name "/secure/auw2/d1/dotnetiis/${parameterStoreAppName}/connectionstrings" | Select -ExpandProperty Value | Out-File -FilePath "$rootDir\ConnectionStrings.config"
    }
    "aue1t1z1cdg${deploymentGroupAppName}releasedeploy" {
        Rename-Item -path "$rootDir\app.test.config" -NewName BrightMls.Enterprise.MatrixClientNotifications.exe -Force
        Get-SSMParameter -WithDecryption 1 -Name "/secure/aue1/t1/dotnetiis/${parameterStoreAppName}/connectionstrings" | Select -ExpandProperty Value | Out-File -FilePath "$rootDir\ConnectionStrings.config"
    }
    "auw2t1z1cdg${deploymentGroupAppName}releasedeploy" {
        Rename-Item -path "$rootDir\app.test.config" -NewName BrightMls.Enterprise.MatrixClientNotifications.exe -Force
        Get-SSMParameter -WithDecryption 1 -Name "/secure/auw2/t1/dotnetiis/${parameterStoreAppName}/connectionstrings" | Select -ExpandProperty Value | Out-File -FilePath "$rootDir\ConnectionStrings.config"
    }
    "aue1p1z1cdg${deploymentGroupAppName}releasedeploy" {
        Rename-Item -path "$rootDir\app.prod.config" -NewName BrightMls.Enterprise.MatrixClientNotifications.exe -Force
        Get-SSMParameter -WithDecryption 1 -Name "/secure/aue1/p1/dotnetiis/${parameterStoreAppName}/connectionstrings" | Select -ExpandProperty Value | Out-File -FilePath "$rootDir\ConnectionStrings.config"
    }
    "auw2p1z1cdg${deploymentGroupAppName}releasedeploy" {
        Rename-Item -path "$rootDir\app.prod.config" -NewName BrightMls.Enterprise.MatrixClientNotifications.exe -Force
        Get-SSMParameter -WithDecryption 1 -Name "/secure/auw2/p1/dotnetiis/${parameterStoreAppName}/connectionstrings" | Select -ExpandProperty Value | Out-File -FilePath "$rootDir\ConnectionStrings.config"
    }
    default { Write-Warning "$env:DEPLOYMENT_GROUP_NAME not valid." }
}
