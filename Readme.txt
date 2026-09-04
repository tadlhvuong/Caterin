- Các cấu hình như đăng nhập fb, gg, sql: được lưu theo key user secrets.
- Time life jwt token: access token: 15', refresh token: 1d, refresh token have remember: 30d. setup in file appsetting.json
- Time life token: confirm email: 24h; còn lại như change pass, reset pass, confim p
- sau delop cầm thêm các biến even như sau trong service (os: centos):
[Unit]
Description=HKMain

[Service]
WorkingDirectory=/var/www/hkmain
ExecStart=/usr/bin/dotnet /var/www/hkmain/HKMain.dll

Environment="Jwt__Secret=xxxxxxxx"
Environment="Email__Password=xxxxxxxx"
Environment="Sms__ApiKey=xxxxxxxx"

Restart=always

[Install]
WantedBy=multi-user.target

- data-asset-path: path resource
- data-app-style-img (style: light/dark): change image follow style web


check valid order-items-adrress-history

SELECT
    o."OrderCode",
    o."SubTotal",
    COALESCE(SUM(oi."Total"), 0) AS "CalculatedSubTotal",
    o."DiscountAmount",
    o."ShippingAmount",
    o."TotalAmount",
    COALESCE(SUM(oi."Total"), 0)
        - o."DiscountAmount"
        + o."ShippingAmount" AS "CalculatedTotal",
    CASE
        WHEN
            o."SubTotal" = COALESCE(SUM(oi."Total"), 0)
            AND
            o."TotalAmount" =
                o."SubTotal"
                - o."DiscountAmount"
                + o."ShippingAmount"
        THEN true
        ELSE false
    END AS "IsValid"
FROM "Orders" AS o
LEFT JOIN "OrderItems" AS oi
    ON oi."OrderId" = o."Id"
WHERE o."OrderCode" LIKE 'ORD202608%'
GROUP BY
    o."Id",
    o."OrderCode",
    o."SubTotal",
    o."DiscountAmount",
    o."ShippingAmount",
    o."TotalAmount"
ORDER BY
    o."OrderCode";