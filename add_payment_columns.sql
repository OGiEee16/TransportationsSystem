-- Add Payment Columns to Bookings Table
-- Run this script to add payment functionality to the transportation system

USE transportationsystem;

-- Add payment_amount column
ALTER TABLE bookings 
ADD COLUMN payment_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00 AFTER driver_status;

-- Add payment_status column
ALTER TABLE bookings 
ADD COLUMN payment_status VARCHAR(20) NOT NULL DEFAULT 'UNPAID' AFTER payment_amount;

-- Add trip_type column
ALTER TABLE bookings 
ADD COLUMN trip_type VARCHAR(20) NOT NULL DEFAULT 'ONE_WAY' AFTER payment_status;

-- Add trip_days column
ALTER TABLE bookings 
ADD COLUMN trip_days INT NOT NULL DEFAULT 1 AFTER trip_type;

-- Add indexes for better query performance
ALTER TABLE bookings 
ADD INDEX idx_payment_status (payment_status);

ALTER TABLE bookings 
ADD INDEX idx_trip_type (trip_type);

-- Verify the columns were added
DESCRIBE bookings;

-- Show sample of bookings with new columns
SELECT id, user_id, origin, destination, trip_type, trip_days, payment_amount, payment_status, status 
FROM bookings 
LIMIT 5;

-- Optional: Update existing bookings with default payment amounts (150 PHP minimum)
UPDATE bookings 
SET payment_amount = 150.00, payment_status = 'UNPAID', trip_type = 'ONE_WAY', trip_days = 1
WHERE payment_amount = 0.00;

SELECT 'Payment columns added successfully!' AS Status;
