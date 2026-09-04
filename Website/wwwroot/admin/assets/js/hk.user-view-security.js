//window.UserSecurity = {

//    init() {

//        if (e.target.id === 'tab-content') {
//            window.Helpers.initPasswordToggle();
//        }

//        $('#btnGeneratePassword').on('click', function () {
//            console.log('generate');
//            const password = generatePassword();

//            $('#newPassword').val(password);
//            $('#confirmPassword').val(password);
//        });

//        const form = document.querySelector("#changePasswordForm");

//        if (!form)
//            return;

//        AppValidation.create(form, {
//            fields: {
//                //NewPassword: AppRules.password({
//                //    required: "Vui lòng nhập mật khẩu mới.",
//                //    password: "Mật khẩu phải có tối thiểu 8 ký tự."
//                //}),

//                //ConfirmPassword: AppRules.confirmPassword(
//                //    form,
//                //    "NewPassword",
//                //    {
//                //        equalTo: "Mật khẩu xác nhận không khớp."
//                //    }
//                //)
//                newPassword: AppRules.password(),
//                confirmPassword: AppRules.confirm("newPassword")
//            }
//        });
//    }
//};

//document.addEventListener("DOMContentLoaded", () => {
//    UserSecurity.init();
//    $('#btnGeneratePassword').on('click', function () {
//        console.log('generate');
//        const password = generatePassword();

//        $('#newPassword').val(password);
//        $('#confirmPassword').val(password);
//    });
//});

