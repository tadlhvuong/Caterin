namespace Shared.Enums
{
    public enum ActivityType
    {
        None,

        Login,
        Register,
        Logout,
        AccessDenied,

        ChangePass,
        ChangeEmail,
        ChangePhone,

        VerifyEmail,
        VerifyPhone,
        RequestOTP,

        ExternalAddLogin,
        ExternalRemoveLogin,
        ExternalSetPassword,

        ForgotPassword, 
        ResetPassByEmail,
        ResetPassByPhone,

        LockAccount,
        UnlockAccount,
        DeleteAccount,
        SuspendAccount,
        UnsuspendAccount,

        AssignRole,
        AssignPermission,

        CreateData,
        UpdateData,
        DeleteData,

        UploadFiles,
        RemoveFiles,

        Publish,
        UnPublish,
        Import,
        Export,

        OrderCreated,
        OrderCancelled,
        OrderCompleted,

        PaymentSuccess,
        PaymentFailed
    }
}
