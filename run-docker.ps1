docker run --rm johannesegger/http-ping `
    --url "http://example.com" `
    --request-method "get" `
    --interval 00:01:00 `
    --request-headers "Authorization=Bearer abcdefghij" "User-Agent=MyBrowser/1.0.0" `
    --cookies "JSESSIONID=123456789" "PHPSESSID=987654321"