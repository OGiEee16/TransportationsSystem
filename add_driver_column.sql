-- Manual Database Update Script
-- Run this if the automatic migration doesn't work or if you need to update the database manually

USE transportationsystem;

-- Check if driver_id column exists
SELECT COUNT(*) 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'transportationsystem' 
  AND TABLE_NAME = 'bookings' 
  AND COLUMN_NAME = 'driver_id';

-- Add driver_id column to bookings table (only run if the above query returns 0)
ALTER TABLE bookings 
ADD COLUMN driver_id INT NULL,
ADD INDEX idx_driver_id (driver_id);

-- Check if driver_status column exists
SELECT COUNT(*) 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'transportationsystem' 
  AND TABLE_NAME = 'bookings' 
  AND COLUMN_NAME = 'driver_status';

-- Add driver_status column to bookings table (only run if the above query returns 0)
ALTER TABLE bookings 
ADD COLUMN driver_status VARCHAR(50) NULL DEFAULT 'PENDING_DRIVER_RESPONSE';

-- Update existing bookings with driver assigned but no status
UPDATE bookings 
SET driver_status = 'PENDING_DRIVER_RESPONSE' 
WHERE driver_id IS NOT NULL AND driver_status IS NULL;

-- Verify the columns were added
DESCRIBE bookings;

-- Optionally, you can see all bookings with the new columns
SELECT id, user_id, vehicle_id, origin, destination, schedule, status, driver_id, driver_status 
FROM bookings 
LIMIT 5;
