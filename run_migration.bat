@echo off
echo ========================================
echo Payment System Database Migration
echo ========================================
echo.
echo This script will add payment columns to the bookings table.
echo.
echo Please enter your MySQL root password when prompted.
echo.
pause

mysql -u root -p transportationsystem < add_payment_columns.sql

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo SUCCESS! Payment columns added.
    echo ========================================
    echo.
    echo You can now run your application!
    echo.
) else (
    echo.
    echo ========================================
    echo ERROR: Migration failed!
    echo ========================================
    echo.
    echo Please check:
    echo 1. MySQL is running
    echo 2. Database 'transportationsystem' exists
    echo 3. Your password is correct
    echo.
)

pause
