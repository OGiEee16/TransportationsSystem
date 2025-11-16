-- Payment System Quick Reference Queries
-- Use these queries to check and verify the payment system

USE transportationsystem;

-- 1. Check if payment columns exist
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE, 
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'bookings' 
  AND COLUMN_NAME IN ('payment_amount', 'payment_status', 'trip_type', 'trip_days')
ORDER BY ORDINAL_POSITION;

-- 2. View all bookings with payment information
SELECT 
    b.id,
    u.full_name AS passenger_name,
    b.origin,
    b.destination,
    b.trip_type,
    b.trip_days,
    CONCAT('?', FORMAT(b.payment_amount, 2)) AS payment_amount,
    b.payment_status,
    b.status AS booking_status,
    d.full_name AS driver_name,
    b.driver_status,
    b.schedule,
    b.created_at
FROM bookings b
LEFT JOIN users u ON b.user_id = u.id
LEFT JOIN users d ON b.driver_id = d.id
ORDER BY b.created_at DESC;

-- 3. Payment summary by status
SELECT 
    payment_status,
    COUNT(*) AS total_bookings,
    SUM(payment_amount) AS total_amount,
    AVG(payment_amount) AS avg_amount,
    MIN(payment_amount) AS min_amount,
    MAX(payment_amount) AS max_amount
FROM bookings
GROUP BY payment_status;

-- 4. Payment summary by trip type
SELECT 
    trip_type,
    COUNT(*) AS total_bookings,
    SUM(payment_amount) AS total_revenue,
    AVG(payment_amount) AS avg_price,
    AVG(trip_days) AS avg_days
FROM bookings
GROUP BY trip_type
ORDER BY total_revenue DESC;

-- 5. Unpaid bookings that are approved
SELECT 
    b.id,
    u.full_name AS passenger,
    b.origin,
    b.destination,
    CONCAT('?', FORMAT(b.payment_amount, 2)) AS amount_due,
    b.schedule,
    DATEDIFF(b.schedule, NOW()) AS days_until_trip,
    d.full_name AS assigned_driver
FROM bookings b
LEFT JOIN users u ON b.user_id = u.id
LEFT JOIN users d ON b.driver_id = d.id
WHERE b.payment_status = 'UNPAID' 
  AND b.status = 'APPROVED'
ORDER BY b.schedule ASC;

-- 6. Revenue by driver
SELECT 
    d.full_name AS driver_name,
    COUNT(*) AS total_trips,
    SUM(CASE WHEN b.payment_status = 'PAID' THEN b.payment_amount ELSE 0 END) AS paid_revenue,
    SUM(CASE WHEN b.payment_status = 'UNPAID' THEN b.payment_amount ELSE 0 END) AS pending_revenue,
    SUM(b.payment_amount) AS total_revenue
FROM bookings b
INNER JOIN users d ON b.driver_id = d.id
WHERE b.status = 'APPROVED'
GROUP BY d.id, d.full_name
ORDER BY total_revenue DESC;

-- 7. Popular routes with average payment
SELECT 
    CONCAT(origin, ' ? ', destination) AS route,
    COUNT(*) AS trip_count,
    AVG(payment_amount) AS avg_payment,
    MIN(payment_amount) AS min_payment,
    MAX(payment_amount) AS max_payment,
    SUM(payment_amount) AS total_revenue
FROM bookings
GROUP BY origin, destination
HAVING trip_count > 0
ORDER BY trip_count DESC
LIMIT 10;

-- 8. Day trip statistics
SELECT 
    trip_days,
    COUNT(*) AS booking_count,
    AVG(payment_amount) AS avg_price,
    SUM(payment_amount) AS total_revenue
FROM bookings
WHERE trip_type = 'DAY_TRIP'
GROUP BY trip_days
ORDER BY trip_days ASC;

-- 9. Payment collection rate
SELECT 
    'Total Bookings' AS metric,
    COUNT(*) AS value,
    CONCAT('?', FORMAT(SUM(payment_amount), 2)) AS total_amount
FROM bookings
UNION ALL
SELECT 
    'Paid Bookings' AS metric,
    COUNT(*) AS value,
    CONCAT('?', FORMAT(SUM(payment_amount), 2)) AS total_amount
FROM bookings
WHERE payment_status = 'PAID'
UNION ALL
SELECT 
    'Unpaid Bookings' AS metric,
    COUNT(*) AS value,
    CONCAT('?', FORMAT(SUM(payment_amount), 2)) AS total_amount
FROM bookings
WHERE payment_status = 'UNPAID'
UNION ALL
SELECT 
    'Collection Rate' AS metric,
    CONCAT(
        FORMAT(
            (COUNT(CASE WHEN payment_status = 'PAID' THEN 1 END) * 100.0 / COUNT(*)), 
            2
        ),
        '%'
    ) AS value,
    '-' AS total_amount
FROM bookings;

-- 10. Monthly revenue report
SELECT 
    DATE_FORMAT(schedule, '%Y-%m') AS month,
    COUNT(*) AS total_bookings,
    SUM(CASE WHEN payment_status = 'PAID' THEN 1 ELSE 0 END) AS paid_bookings,
    SUM(CASE WHEN payment_status = 'UNPAID' THEN 1 ELSE 0 END) AS unpaid_bookings,
    CONCAT('?', FORMAT(SUM(CASE WHEN payment_status = 'PAID' THEN payment_amount ELSE 0 END), 2)) AS revenue_collected,
    CONCAT('?', FORMAT(SUM(payment_amount), 2)) AS total_potential_revenue
FROM bookings
WHERE status != 'CANCELLED'
GROUP BY DATE_FORMAT(schedule, '%Y-%m')
ORDER BY month DESC;

-- 11. Update sample data with payment information (for testing)
-- Uncomment to update existing bookings with realistic payment data
/*
UPDATE bookings 
SET 
    trip_type = 'ONE_WAY',
    trip_days = 1,
    payment_amount = 150.00,
    payment_status = 'UNPAID'
WHERE payment_amount = 0.00 OR payment_amount IS NULL;
*/

-- 12. Find bookings needing driver payment confirmation
SELECT 
    b.id,
    u.full_name AS passenger,
    d.full_name AS driver,
    b.origin,
    b.destination,
    CONCAT('?', FORMAT(b.payment_amount, 2)) AS amount,
    b.driver_status,
    b.schedule
FROM bookings b
INNER JOIN users u ON b.user_id = u.id
INNER JOIN users d ON b.driver_id = d.id
WHERE b.payment_status = 'UNPAID'
  AND b.driver_status = 'ACCEPTED'
  AND b.status = 'APPROVED'
ORDER BY b.schedule ASC;
