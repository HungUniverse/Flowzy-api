# Khôi phục SU26 để tiếp tục test local

Thực hiện ngày 28/09/2026 theo yêu cầu xóa FA26, mở lại SU26 và xác nhận kích hoạt lại nhóm/sinh viên đã archive.

- Đã sao lưu DB trước khi thay đổi: `.tools/term-recovery/flowzy-before-reopen-su26-20260928-011307-fdc03750.dump`, 389.008 byte; kiểm tra danh mục archive thành công.
- FA26 (ID 2) không có nhóm, milestone hoặc feedback. Đã xóa đúng bản ghi kỳ này, không xóa dây chuyền dữ liệu nghiệp vụ.
- SU26 (ID 1) chuyển CLOSED → OPEN; bỏ thời gian/người đóng hiện tại. Thông tin trước thay đổi vẫn có trong backup.
- Đã kích hoạt lại 231 nhóm và 1.270 cặp student profile/account thuộc SU26. Các account/profile này cùng bị archive tại `2026-09-27 18:07:55.421365+00`.
- Không đổi password, role, email, membership, mentor/instructor assignment, task, milestone, grade hoặc meeting.
- Không kích hoạt tài khoản instructor INACTIVE hay sửa tài khoản ngoài phạm vi SU26.
- Giữ nguyên 1.221 feedback PENDING sinh ra khi đóng kỳ. API không cho gửi feedback trong kỳ OPEN; vì vậy bảng Terms vẫn có thể hiển thị 0/1221.
- Không tự chuyển lại PENDING cho invitation/join request đã bị hủy khi đóng kỳ. Cần gửi lời mời/yêu cầu mới nếu muốn test lại các luồng này.
- Token refresh cũ đã bị thu hồi khi archive không được phục hồi. Người dùng cần đăng xuất/đăng nhập lại nếu còn lỗi phiên đăng nhập.

BE không có endpoint reopen theo contract Java hiện tại. Việc khôi phục được thực hiện bằng hai giao dịch SQL local có kiểm tra điều kiện và khóa bảng; không thêm hoặc thay đổi contract API.

Đã kiểm tra API: danh sách kỳ chỉ còn SU26 OPEN với 231 nhóm; nhóm UAT A ACTIVE, `studentReadOnly=false`; S1 đăng nhập bằng mật khẩu UAT hiện hữu được, xem nhóm và grade matrix được. Chưa thực hiện lại toàn bộ bài test FE.

Nếu cần phục hồi bản ghi FA26 hoặc trạng thái trước thao tác, bản dump trước khôi phục vẫn còn. Không restore toàn DB đang hoạt động nếu chưa đánh giá dữ liệu phát sinh sau mốc backup.
